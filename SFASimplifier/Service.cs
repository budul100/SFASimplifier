using System;
using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using ProgressWatcher;
using ProgressWatcher.Interfaces;
using SFASimplifier.Extensions;
using SFASimplifier.Factories;
using SFASimplifier.Models;
using SFASimplifier.Repositories;
using SFASimplifier.Writers;

namespace SFASimplifier
{
    public class Service
    {
        #region Private Fields

        private const double StatusWeightDeterminingLinks = 0.2;
        private const double StatusWeightDeterminingSegments = 0.5;
        private const double StatusWeightLoadingFiles = 0.2;

        private readonly ChainFactory chainFactory;
        private readonly FeatureWriter featureWriter;
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

            wayFactory = new WayFactory(
                geometryFactory: geometryFactory);

            pointFactory = new PointFactory(
                geometryFactory: geometryFactory);

            locationFactory = new LocationFactory(
                geometryFactory: geometryFactory,
                maxDistance: options.LocationsDistanceToOthers,
                fuzzyScore: options.LocationsFuzzyScore);

            segmentFactory = new SegmentFactory(
                geometryFactory: geometryFactory,
                pointFactory: pointFactory,
                locationFactory: locationFactory,
                keyAttributes: options.PointAttributesKey,
                distanceNodeToLine: options.LocationsDistanceToLine);

            var angleMin = AngleUtility.ToRadians(options.LinksAngleMin);

            chainFactory = new ChainFactory(
                geometryFactory: geometryFactory,
                angleMin: angleMin);

            linkFactory = new LinkFactory(
                geometryFactory: geometryFactory,
                angleMin: angleMin,
                lengthSplit: options.LinksLengthSplit);

            featureWriter = new FeatureWriter(
                geometryFactory: geometryFactory,
                wayFactory: wayFactory);
        }

        #endregion Public Constructors

        #region Public Methods

        public void Run(IEnumerable<string> inputPaths, string outputPath)
        {
            using var parentPackage = progressWatcher.Initialize(
                allSteps: 4,
                status: "Merge SFA data.");

            LoadFiles(
                inputPaths: inputPaths,
                parentPackage: parentPackage);

            DetermineSegments(
                parentPackage: parentPackage);

            DetermineLinks(
                parentPackage: parentPackage);

            WriteFile(
                outputPath: outputPath,
                parentPackage: parentPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void DetermineLinks(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 3,
                status: "Determining links.",
                weight: StatusWeightDeterminingLinks);

            locationFactory.Tidy(
                segments: segmentFactory.Segments,
                parentPackage: infoPackage);

            chainFactory.Load(
                segments: segmentFactory.Segments,
                parentPackage: infoPackage);
            linkFactory.Load(
                chains: chainFactory.Chains,
                parentPackage: infoPackage);
        }

        private void DetermineSegments(IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Determining segments.",
                weight: StatusWeightDeterminingSegments);

            segmentFactory.Load(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
        }

        private void LoadFeatures(string inputPath, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                steps: 6,
                status: "Loading features.");

            var collectionRepository = new CollectionRepository();

            collectionRepository.Load(
                file: inputPath,
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
                attributesKey: options.LineAttributesKey,
                lineFilters: options.LineFilters,
                parentPackage: infoPackage);
            pointFactory.LoadWays(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
        }

        private void LoadFiles(IEnumerable<string> inputPaths, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                items: inputPaths,
                status: "Loading files.",
                weight: StatusWeightLoadingFiles);

            foreach (var inputPath in inputPaths)
            {
                LoadFeatures(
                    inputPath: inputPath,
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

        private void WriteFile(string outputPath, IPackage parentPackage)
        {
            using var infoPackage = parentPackage.GetPackage(
                status: "Writing collection.");

            featureWriter.Write(
                path: outputPath,
                parentPackage: infoPackage);
        }

        #endregion Private Methods
    }
}