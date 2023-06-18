using NetTopologySuite.Geometries;
using ProgressWatcher;
using ProgressWatcher.Interfaces;
using SFASimplifier.Factories;
using SFASimplifier.Repositories;
using SFASimplifier.Writers;
using System;
using System.Collections.Generic;

namespace SFASimplifier
{
    public class Service
    {
        #region Private Fields

        private const double StatusWeightDeterminingLinks = 0.2;
        private const double StatusWeightDeterminingSegments = 0.6;
        private const double StatusWeightLoadingFeatures = 0.1;
        private const double StatusWeightWritingFeatures = 0.1;

        private readonly ChainFactory chainFactory;
        private readonly CollectionRepository collectionRepository;
        private readonly FeatureWriter featureWriter;
        private readonly GeometryFactory geometryFactory;
        private readonly FeatureRepository lineRepository;
        private readonly LinkFactory linkFactory;
        private readonly LocationFactory locationFactory;
        private readonly Action<double, string> onProgressChange;
        private readonly PointFactory pointFactory;
        private readonly FeatureRepository pointRepository;
        private readonly Watcher progressWatcher;
        private readonly SegmentFactory segmentFactory;
        private readonly WayFactory wayFactory;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEnumerable<OgcGeometryType> pointTypes, IEnumerable<(string, string)> pointAttributesCheck,
            IEnumerable<(string, string)> pointAttributesFilter, IEnumerable<OgcGeometryType> lineTypes,
            IEnumerable<(string, string)> lineAttributesCheck, IEnumerable<(string, string)> lineAttributesFilter,
            int locationsDistanceToOthers, double locationsFuzzyScore, string locationsKeyAttribute,
            int pointsDistanceMaxToLine, double linksAngleMin, double linksDetourMax, Action<double, string> onProgressChange)
        {
            this.onProgressChange = onProgressChange;

            collectionRepository = new CollectionRepository();

            pointRepository = new FeatureRepository(
                types: pointTypes,
                checkAttributes: pointAttributesCheck,
                filterAttributes: pointAttributesFilter);

            lineRepository = new FeatureRepository(
                types: lineTypes,
                checkAttributes: lineAttributesCheck,
                filterAttributes: lineAttributesFilter);

            geometryFactory = new GeometryFactory();

            wayFactory = new WayFactory(
                geometryFactory: geometryFactory);

            pointFactory = new PointFactory(
                geometryFactory: geometryFactory);

            locationFactory = new LocationFactory(
                geometryFactory: geometryFactory,
                maxDistance: locationsDistanceToOthers,
                fuzzyScore: locationsFuzzyScore);

            segmentFactory = new SegmentFactory(
                geometryFactory: geometryFactory,
                pointFactory: pointFactory,
                locationFactory: locationFactory,
                keyAttribute: locationsKeyAttribute,
                distanceNodeToLine: pointsDistanceMaxToLine);

            chainFactory = new ChainFactory(
                geometryFactory: geometryFactory,
                angleMin: linksAngleMin);

            linkFactory = new LinkFactory(
                geometryFactory: geometryFactory,
                angleMin: linksAngleMin,
                detourMax: linksDetourMax);

            featureWriter = new FeatureWriter(
                geometryFactory: geometryFactory,
                wayFactory: wayFactory);

            progressWatcher = new Watcher();

            progressWatcher.PropertyChanged += OnProgressChanged;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Run(string importPath, string exportPath)
        {
            var parentPackage = progressWatcher.Initialize(
                allSteps: 4,
                status: "Merge SFA data.");

            LoadFeatures(
                importPath: importPath,
                parentPackage: parentPackage);

            DetermineSegments(
                parentPackage: parentPackage);

            DetermineLinks(
                parentPackage: parentPackage);

            WriteFeatures(
                exportPath: exportPath,
                parentPackage: parentPackage);
        }

        #endregion Public Methods

        #region Private Methods

        private void DetermineLinks(IPackage parentPackage)
        {
            var infoPackage = parentPackage.GetPackage(
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
            var infoPackage = parentPackage.GetPackage(
                status: "Determining segments.",
                weight: StatusWeightDeterminingSegments);

            segmentFactory.Load(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
        }

        private void LoadFeatures(string importPath, IPackage parentPackage)
        {
            var infoPackage = parentPackage.GetPackage(
                steps: 6,
                status: "Loading features.",
                weight: StatusWeightLoadingFeatures);

            collectionRepository.Load(
                file: importPath,
                parentPackage: infoPackage);

            pointRepository.Load(
                collection: collectionRepository.Collection,
                parentPackage: infoPackage);
            lineRepository.Load(
                collection: collectionRepository.Collection,
                parentPackage: infoPackage);

            wayFactory.Load(
                lines: lineRepository.Features,
                parentPackage: infoPackage);

            pointFactory.LoadPoints(
                features: pointRepository.Features,
                parentPackage: infoPackage);
            pointFactory.LoadWays(
                ways: wayFactory.Ways,
                parentPackage: infoPackage);
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

        private void WriteFeatures(string exportPath, IPackage parentPackage)
        {
            var infoPackage = parentPackage.GetPackage(
                status: "Writing collection.",
                weight: StatusWeightWritingFeatures);

            featureWriter.Write(
                path: exportPath,
                parentPackage: infoPackage);
        }

        #endregion Private Methods
    }
}