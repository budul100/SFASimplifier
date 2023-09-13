using NetTopologySuite.Geometries;
using ProgressWatcher;
using ProgressWatcher.Interfaces;
using SFASimplifier.Simplifier.Extensions;
using SFASimplifier.Simplifier.Factories;
using SFASimplifier.Simplifier.Models;
using SFASimplifier.Simplifier.Repositories;
using SFASimplifier.Simplifier.Writers;
using Simplifier.Repositories;
using Simplifier.Writers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier.Simplifier
{
    public class Service
    {
        #region Private Fields

        private const double StatusWeightDeterminingConnections = 0.2;
        private const double StatusWeightDeterminingSegments = 0.5;
        private const double StatusWeightLoadingFeatures = 0.1;
        private const double StatusWeightLoadingStops = 0.1;

        private readonly ChainFactory chainFactory;
        private readonly GeometryFactory geometryFactory;
        private readonly LinkFactory linkFactory;
        private readonly LocationFactory locationFactory;
        private readonly Action<double, string> onProgressChange;
        private readonly Options options;
        private readonly PointFactory pointFactory;
        private readonly Watcher progressWatcher;
        private readonly SegmentFactory segmentFactory;
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public Service(Options options, Action<double, string> onProgressChange)
        {
            this.options = options;
            this.onProgressChange = onProgressChange;

            progressWatcher = new Watcher();
            progressWatcher.PropertyChanged += OnProgressChanged;
            progressWatcher.ProgressCompleted += OnProgressCompleted;

            geometryFactory = new GeometryFactory();

            var bboxEnvelope = options.BBoxFilter.GetEnvelope();

            wayFactory = new WayFactory(
                geometryFactory: geometryFactory,
                attributesKey: options.LineAttributesKey,
                lineFilters: options.LineAttributesKeyFilter,
                bboxEnvelope: bboxEnvelope);

            pointFactory = new PointFactory(
                geometryFactory: geometryFactory,
                bboxEnvelope: bboxEnvelope);

            locationFactory = new LocationFactory(
                geometryFactory: geometryFactory,
                pointFactory: pointFactory,
                maxDistanceNamed: options.DistanceBetweenLocationsNamed,
                maxDistanceAnonymous: options.DistanceBetweenLocationsAnonymous,
                fuzzyScore: options.LocationsFuzzyScore);

            segmentFactory = new SegmentFactory(
                geometryFactory: geometryFactory,
                pointFactory: pointFactory,
                locationFactory: locationFactory,
                keyAttributes: options.PointAttributesKey,
                maxDistanceToCapture: options.DistanceToCapture);

            chainFactory = new ChainFactory(
                geometryFactory: geometryFactory,
                locationFactory: locationFactory,
                angleMin: options.AngleMinMerge,
                lengthSplit: options.LinksLengthSplit);

            linkFactory = new LinkFactory(
                geometryFactory: geometryFactory,
                locationFactory: locationFactory,
                angleMin: options.AngleMinLinks,
                lengthSplit: options.LinksLengthSplit,
                distanceToJunction: options.DistanceToJunction,
                distanceToMerge: options.DistanceToMerge);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Run()
        {
            if (options.PathsInFeatures.Contains(options.PathOutFeatures))
            {
                throw new ApplicationException(
                    message: $"The output path {options.PathOutFeatures} is a path of an input file, too. This is not allowed.");
            }

            if (options.PathsInStops.Contains(options.PathOutStops))
            {
                throw new ApplicationException(
                    message: $"The output path {options.PathOutFeatures} is a path of an input file, too. This is not allowed.");
            }

            using var parentPackage = progressWatcher.Initialize(
                allSteps: 5,
                status: "Simplify SFA data.");

            LoadStops(
                paths: options.PathsInStops,
                parentPackage: parentPackage);

            LoadFeatures(
                paths: options.PathsInFeatures,
                parentPackage: parentPackage);

            DetermineSegments(
                parentPackage: parentPackage);

            DetermineLinks(
                parentPackage: parentPackage);

            WriteFiles(
                pathGeoJson: options.PathOutFeatures,
                pathStops: options.PathOutStops,
                parentPackage: parentPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void DetermineLinks(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 5,
                status: "Determine links.",
                weight: StatusWeightDeterminingConnections);

            chainFactory.Load(
                segments: segmentFactory.Segments,
                parentPackage: infoPackage);

            locationFactory.Tidy(
                parentPackage: infoPackage);

            linkFactory.Load(
                chains: chainFactory.Chains,
                parentPackage: infoPackage);

            linkFactory.Tidy(
                parentPackage: infoPackage);
        }

        private void DetermineSegments(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Determine segments.",
                weight: StatusWeightDeterminingSegments);

            segmentFactory.Load(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
        }

        private void ImportFeatures(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 6,
                status: "Load features.");

            var collectionRepository = new CollectionRepository();

            collectionRepository.Load(
                path: path,
                parentPackage: infoPackage);

            var pointAttributesFilter = options.PointAttributesFilter
                .GetKeyValuePairs().ToArray();

            var pointRepository = new FeatureRepository(
                types: options.PointTypes,
                attributesKey: options.PointAttributesKey,
                attributesFilter: pointAttributesFilter);

            pointRepository.Load(
                collection: collectionRepository.Collection,
                parentPackage: infoPackage);
            pointFactory.LoadPoints(
                features: pointRepository.Features,
                parentPackage: infoPackage);

            var lineAttributesFilter = options.LineAttributesFilter
                .GetKeyValuePairs().ToArray();

            var lineRepository = new FeatureRepository(
                types: options.LineTypes,
                attributesKey: options.LineAttributesKey,
                attributesFilter: lineAttributesFilter);

            lineRepository.Load(
                collection: collectionRepository.Collection,
                parentPackage: infoPackage);
            wayFactory.Load(
                lines: lineRepository.Features,
                parentPackage: infoPackage);
            pointFactory.LoadWays(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
        }

        private void ImportStops(string path, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 2,
                status: "Load stops.");

            var stopRepository = new StopRepository(
                stopDelimiter: options.StopDelimiter);

            stopRepository.Load(
                path: path,
                parentPackage: infoPackage);

            pointFactory.LoadStops(
                stops: stopRepository.Stops,
                parentPackage: infoPackage);
        }

        private void LoadFeatures(IEnumerable<string> paths, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: paths,
                status: "Load feature files.",
                weight: StatusWeightLoadingFeatures);

            foreach (var path in paths)
            {
                ImportFeatures(
                    path: path,
                    parentPackage: infoPackage);
            }
        }

        private void LoadStops(IEnumerable<string> paths, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: paths,
                status: "Load stop files.",
                weight: StatusWeightLoadingStops);

            foreach (var path in paths)
            {
                ImportStops(
                    path: path,
                    parentPackage: infoPackage);
            }
        }

        private void OnProgressChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (onProgressChange != default)
            {
                var text = $"{progressWatcher.Status} ({progressWatcher.ProgressTip * 100:0}%)";

                onProgressChange.Invoke(
                    arg1: progressWatcher.ProgressAll,
                    arg2: text);
            }
        }

        private void OnProgressCompleted(object sender, EventArgs e)
        {
            if (onProgressChange != default)
            {
                progressWatcher.PropertyChanged -= OnProgressChanged;

                onProgressChange.Invoke(
                    arg1: 1,
                    arg2: default);
            }
        }

        private void WriteFiles(string pathGeoJson, string pathStops, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 2,
                status: "Write outputs.");

            var featureWriter = new FeatureWriter(
                wayFactory: wayFactory,
                preventMergingAttributes: options.PreventMergingAttributes);

            featureWriter.Write(
                path: pathGeoJson,
                parentPackage: infoPackage);

            var stopWriter = new StopWriter(
                locationFactory: locationFactory,
                stopDelimiter: options.StopDelimiter);

            stopWriter.Write(
                path: pathStops,
                parentPackage: infoPackage);
        }

        #endregion Private Methods
    }
}