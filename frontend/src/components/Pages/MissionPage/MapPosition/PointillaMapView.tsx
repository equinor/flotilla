import { MapContainer } from 'react-leaflet'
import AuthTileLayer from './PointillaMap'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useState } from 'react'
import { PointillaMapInfo } from 'models/PointillaMapInfo'
import { Task, TaskStatus } from 'models/Task'
import styled, { createGlobalStyle } from 'styled-components'
import { MapCompass } from 'utils/MapCompass'
import { phone_width } from 'utils/constants'
import { getColorsFromTaskStatus } from 'utils/MarkerStyles'

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
    tasks?: Task[]
}

export default function PlantMap({ plantCode, floorId, tasks }: PlantMapProps) {
    const [map, setMap] = useState<L.Map | null>(null)
    const [mapInfo, setMapInfo] = useState<PointillaMapInfo | undefined>(undefined)

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

    const orderTasksByDrawOrder = (tasks: Task[]) => {
        const isOngoing = (task: Task) => task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused
        const sortedTasks = [...tasks].sort((a, b) => {
            if (a.status === TaskStatus.NotStarted && b.status === TaskStatus.NotStarted)
                return b.taskOrder - a.taskOrder
            else if (isOngoing(a)) return 1
            else if (isOngoing(b)) return -1
            else if (a.status === TaskStatus.NotStarted) return 1
            else if (b.status === TaskStatus.NotStarted) return -1
            return a.taskOrder - b.taskOrder
        })
        return sortedTasks
    }

    const getMarker = (task: Task) => {
        let color = getColorsFromTaskStatus(task.status)

        const marker = L.circleMarker([task.robotPose.position.y, task.robotPose.position.x], {
            radius: 15,
            fillColor: color.fillColor,
            color: 'black',
            weight: 1,
            fillOpacity: 0.8,
        })
            .bindTooltip((task.taskOrder + 1).toString(), {
                permanent: true,
                direction: 'center',
                className: 'circleLabel',
            })
            .addTo(map!)
        return marker
    }

    useEffect(() => {
        if (!tasks?.length || !map) return
        const markers = orderTasksByDrawOrder(tasks).map((task) => getMarker(task))

        const group = L.featureGroup(markers)
        map.fitBounds(group.getBounds())

        return () => markers.forEach((marker) => marker.remove())
    }, [tasks, mapInfo])

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
