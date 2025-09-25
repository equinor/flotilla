import { MapContainer } from 'react-leaflet'
import AuthTileLayer from './PointillaMap'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { PointillaMapInfo } from 'models/PointillaMapInfo'
import styled, { createGlobalStyle } from 'styled-components'
import { MapCompass } from 'utils/MapCompass'
import { phone_width } from 'utils/constants'
import { Mission } from 'models/Mission'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { getRobotMarker, getTaskMarkers } from './PointillaMapMarkers'

const LeafletTooltipStyles = createGlobalStyle`
 
  .leaflet-tooltip.circleLabel {
    background: transparent !important;
    border: none !important;
    box-shadow: none !important;
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

export default function PlantMap({ plantCode, floorId, mission }: PlantMapProps) {
    const { enabledRobots } = useAssetContext()
    const [mapInfo, setMapInfo] = useState<PointillaMapInfo | undefined>(undefined)
    const [map, setMap] = useState<L.Map | null>(null)

    const robot = enabledRobots.find((r) => r.id === mission.robot.id)
    const tasks = mission?.tasks
    const updateIntervalRobotAuraInMS = 50

    const loadMap = async () => {
        if (!map) return
        BackendAPICaller.getFloorMapInfo(plantCode, floorId)
            .then((info) => {
                setMapInfo(info)
                if (info) {
                    const mapWidth = info.xMax - info.xMin
                    const mapHeight = info.yMax - info.yMin
                    const scaleFactorX = info.tileSize / mapWidth
                    const scaleFactorY = info.tileSize / mapHeight
                    const originX = -info.xMin * scaleFactorX
                    const originY = info.yMin * scaleFactorY
                    const customTransformation = new L.Transformation(
                        scaleFactorX,
                        originX,
                        -scaleFactorY,
                        info.tileSize + originY
                    )
                    const plantCrs = L.extend({}, L.CRS.Simple, {
                        transformation: customTransformation,
                    })

                    map.options.crs = plantCrs
                    map.fitBounds([
                        [info.yMin, info.xMin],
                        [info.yMax, info.xMax],
                    ])
                    map.options.minZoom = info.zoomMin
                    map.options.maxZoom = info.zoomMax
                }
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
        if (!robot?.pose || !map) return
        let robotMarkers = getRobotMarker(map, robot.pose)
        const timer = setInterval(() => {
            robotMarkers.forEach((marker) => marker?.remove())
            if (!robot?.pose || !map) return
            robotMarkers = getRobotMarker(map, robot.pose)
        }, updateIntervalRobotAuraInMS)
        return () => {
            clearInterval(timer)
            robotMarkers.forEach((marker) => marker?.remove())
        }
    }, [robot?.pose])

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
