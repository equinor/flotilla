import { MapContainer, Polygon } from 'react-leaflet'
import AuthTileLayer from './PointillaMap'
import 'leaflet/dist/leaflet.css'
import L, { LatLngBoundsExpression } from 'leaflet'
import { useEffect, useState } from 'react'
import { PointillaMapInfo } from 'models/PointillaMapInfo'
import styled, { createGlobalStyle } from 'styled-components'
import { MapCompass } from 'utils/MapCompass'
import { phone_width } from 'utils/constants'
import { Mission } from 'models/Mission'
import { getRobotMarker, getTaskMarkers } from './PointillaMapMarkers'
import { useAllRobotPosesTelemetry, useRobotTelemetry } from 'hooks/useRobotTelemetry'
import { InspectionArea, PolygonPoint } from 'models/InspectionArea'
import 'utils/leaflet-overrides.css'
import { useBackendApi } from 'api/UseBackendApi'
import { useAssetContext } from 'components/Contexts/AssetContext'

const LeafletTooltipStyles = createGlobalStyle`
    .leaflet-tooltip.circleLabel {
    background: transparent !important;
    border: none !important;
    box-shadow: none !important;
    }

    .leaflet-container {
        font-family: Equinor;
        font-size: 16px;
        text-align: center;
    }

    .leaflet-control-zoom {
        box-shadow: none;
        opacity: 0.8;
    }

`
const StyledElements = styled.div`
    display: flex;
    flex-direction: columns;
    align-items: end;
`

const StyledMapContainer = styled(MapContainer)`
    height: 500px;
    width: 500px;

    @media (max-width: ${phone_width}) {
        height: 500px;
        width: 300px;
    }
`

type PlantMapProps = {
    plantCode: string
    floorId: string
    mission: Mission
}

type PlantPolygonMapProps = {
    inspectionArea: InspectionArea
    floorId: string
}

const setMapOptions = (map: L.Map, info: PointillaMapInfo) => {
    const mapWidth = info.xMax - info.xMin
    const mapHeight = info.yMax - info.yMin
    const scaleFactorX = info.tileSize / mapWidth
    const scaleFactorY = info.tileSize / mapHeight
    const originX = -info.xMin * scaleFactorX
    const originY = info.yMin * scaleFactorY
    const customTransformation = new L.Transformation(scaleFactorX, originX, -scaleFactorY, info.tileSize + originY)
    const plantCrs = L.extend({}, L.CRS.Simple, {
        transformation: customTransformation,
    })

    const bounds: LatLngBoundsExpression = [
        [info.yMin, info.xMin],
        [info.yMax, info.xMax],
    ]
    map.options.crs = plantCrs

    map.fitBounds(bounds)
    map.setMaxBounds(bounds)
    map.options.maxBoundsViscosity = 1.0
    map.options.minZoom = info.zoomMin
    map.options.maxZoom = info.zoomMax
}

const updateIntervalRobotAuraInMS = 50

export function PlantMap({ plantCode, floorId, mission }: PlantMapProps) {
    const [mapInfo, setMapInfo] = useState<PointillaMapInfo | undefined>(undefined)
    const [map, setMap] = useState<L.Map | null>(null)
    const { robotPose } = useRobotTelemetry(mission.robot)
    const backendApi = useBackendApi()

    const tasks = mission?.tasks

    const loadMap = async () => {
        if (!map) return
        backendApi
            .getFloorMapInfo(plantCode, floorId)
            .then((info) => {
                setMapInfo(info)
                if (info) setMapOptions(map, info)
            })
            .catch((error) => {
                console.error('Error loading map:', error)
            })
    }

    useEffect(() => {
        loadMap()
    }, [plantCode, floorId, map])

    useEffect(() => {
        if (!tasks?.length || !map) return
        const taskMarkers = getTaskMarkers(map, tasks)

        const group = L.featureGroup(taskMarkers)
        map.fitBounds(group.getBounds())

        return () => {
            taskMarkers.forEach((marker) => marker.remove())
        }
    }, [mapInfo])

    useEffect(() => {
        if (!tasks?.length || !map) return
        const taskMarkers = getTaskMarkers(map, tasks)

        return () => {
            taskMarkers.forEach((marker) => marker.remove())
        }
    }, [tasks])

    useEffect(() => {
        if (!robotPose || !map) return
        let robotMarkers = getRobotMarker(map, robotPose)
        const timer = setInterval(() => {
            robotMarkers.forEach((marker) => marker?.remove())
            if (!robotPose || !map) return
            robotMarkers = getRobotMarker(map, robotPose)
        }, updateIntervalRobotAuraInMS)
        return () => {
            clearInterval(timer)
            robotMarkers.forEach((marker) => marker?.remove())
        }
    }, [robotPose])

    return (
        <div className="map-root">
            <StyledElements>
                <LeafletTooltipStyles />
                <StyledMapContainer ref={setMap} attributionControl={false}>
                    {mapInfo && <AuthTileLayer mapInfo={mapInfo} />}
                </StyledMapContainer>
                <MapCompass />
            </StyledElements>
        </div>
    )
}

export function PlantPolygonMap({ inspectionArea, floorId }: PlantPolygonMapProps) {
    const [mapInfo, setMapInfo] = useState<PointillaMapInfo | undefined>(undefined)
    const [map, setMap] = useState<L.Map | null>(null)
    const backendApi = useBackendApi()

    const { enabledRobots } = useAssetContext()
    const robotIdsInArea = enabledRobots.filter((r) => r.currentInspectionAreaId === inspectionArea.id).map((r) => r.id)
    const { robotIdAndPoses } = useAllRobotPosesTelemetry()
    const robotPoses = robotIdAndPoses
        .filter((IdAndPose) => robotIdsInArea.includes(IdAndPose.robotId))
        .map((IdAndPose) => IdAndPose.pose)

    const plantCode = inspectionArea.plantCode
    const polygon = inspectionArea.areaPolygon?.positions ?? []

    const loadMap = async () => {
        if (!map) return
        backendApi
            .getFloorMapInfo(plantCode, floorId)
            .then((info) => {
                setMapInfo(info)
                if (info) setMapOptions(map, info)
            })
            .catch((error) => {
                console.error('Error loading map:', error)
            })
    }

    const toLeafletPositions = (positions: PolygonPoint[]): [number, number][] => positions.map((p) => [p.y, p.x])

    const positions = polygon ? toLeafletPositions(polygon) : undefined

    useEffect(() => {
        if (robotPoses.length < 1 || !map) return
        let robotMarkers = robotPoses
            .filter((robotPose) => robotPose != undefined)
            .map((robotPose) => getRobotMarker(map, robotPose))
            .flat()
        const timer = setInterval(() => {
            robotMarkers.forEach((marker) => marker?.remove())
            if (robotPoses.length < 1 || !map) return
            robotMarkers = robotPoses
                .filter((robotPose) => robotPose != undefined)
                .map((robotPose) => getRobotMarker(map, robotPose))
                .flat()
        }, updateIntervalRobotAuraInMS)
        return () => {
            clearInterval(timer)
            robotMarkers.forEach((marker) => marker?.remove())
        }
    }, [robotPoses, map])

    useEffect(() => {
        loadMap()
    }, [plantCode, floorId, map])

    useEffect(() => {
        if (!positions || positions.length < 3 || !map) return
        const bounds = L.latLngBounds(positions)
        map.fitBounds(bounds)
    }, [inspectionArea, mapInfo])

    return (
        <div className="map-root">
            <StyledElements>
                <LeafletTooltipStyles />
                <StyledMapContainer ref={setMap} attributionControl={false}>
                    {polygon && positions && (
                        <Polygon
                            positions={positions}
                            pathOptions={{
                                color: 'blue',
                                fillOpacity: 0.2,
                                weight: 1,
                            }}
                        />
                    )}
                    {mapInfo && <AuthTileLayer mapInfo={mapInfo} />}
                </StyledMapContainer>
                <MapCompass />
            </StyledElements>
        </div>
    )
}
