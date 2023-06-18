using NetTopologySuite.Geometries;
using ProgressWatcher;
using SFASimplifier.Factories;
using SFASimplifier.Repositories;
using SFASimplifier.Writers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SFASimplifier
{
    public class Service
    {
        #region Private Fields

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

            progressWatcher.PropertyChanged += OnProgressWatcherChanged;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Run(string importPath, string exportPath)
        {
            var parentPackage = progressWatcher.Initialize(
                allSteps: 11,
                status: "Merge SFA data.");

            collectionRepository.Load(
                file: importPath,
                parentPackage: parentPackage);

            if (collectionRepository.Collection?.Any() == true)
            {
                pointRepository.Load(
                    collection: collectionRepository.Collection,
                    parentPackage: parentPackage);
                lineRepository.Load(
                    collection: collectionRepository.Collection,
                    parentPackage: parentPackage);

                wayFactory.Load(
                    lines: lineRepository.Features,
                    parentPackage: parentPackage);

                pointFactory.LoadPoints(
                    features: pointRepository.Features,
                    parentPackage: parentPackage);
                pointFactory.LoadWays(
                    ways: wayFactory.Ways,
                    parentPackage: parentPackage);

                segmentFactory.Load(
                    ways: wayFactory.Ways,
                    parentPackage: parentPackage);
                locationFactory.Tidy(
                    segments: segmentFactory.Segments,
                    parentPackage: parentPackage);

                chainFactory.Load(
                    segments: segmentFactory.Segments,
                    parentPackage: parentPackage);
                linkFactory.Load(
                    chains: chainFactory.Chains,
                    parentPackage: parentPackage);

                featureWriter.Write(path:
                    exportPath,
                    parentPackage: parentPackage);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void OnProgressWatcherChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (onProgressChange != default)
            {
                onProgressChange.Invoke(
                    arg1: progressWatcher.ProgressAll,
                    arg2: progressWatcher.Status);
            }
        }

        #endregion Private Methods
    }
}