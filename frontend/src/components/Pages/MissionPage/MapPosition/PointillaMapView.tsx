import { MapContainer } from 'react-leaflet'
import AuthTileLayer from './PointillaMap'
import 'leaflet/dist/leaflet.css'
import L from 'leaflet'
import { BackendAPICaller } from 'api/ApiCaller'
import { useEffect, useRef, useState } from 'react'
import { PointillaMapInfo } from 'models/PointillaMapInfo'
import { Task } from 'models/Task'
import styled, { createGlobalStyle } from 'styled-components'
import { MapCompass } from 'utils/MapCompass'
import { phone_width } from 'utils/constants'

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
    const mapRef = useRef<L.Map | null>(null)
    const [mapInfo, setMapInfo] = useState<PointillaMapInfo | undefined>(undefined)
    const [mapReady, setMapReady] = useState<boolean>(false)
    const loadMap = async () => {
        if (!mapReady) return
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
                    if (mapRef.current) {
                        mapRef.current.options.crs = plantCrs
                        mapRef.current.fitBounds([
                            [info.yMin, info.xMin],
                            [info.yMax, info.xMax],
                        ])
                        mapRef.current.options.minZoom = info.zoomMin
                        mapRef.current.options.maxZoom = info.zoomMax
                    }
                }
            })
            .catch((error) => {
                console.error('Error loading map:', error)
            })
    }

    useEffect(() => {
        loadMap()
    }, [plantCode, floorId, mapReady])

    useEffect(() => {
        if (!tasks?.length || !mapRef.current) return
        const markers = tasks.map((task) => {
            const marker = L.circleMarker([task.robotPose.position.y, task.robotPose.position.x], {
                radius: 15,
                fillColor: 'white',
                color: 'black',
                weight: 0,
                fillOpacity: 0.8,
            })
                .bindTooltip((task.taskOrder + 1).toString(), {
                    permanent: true,
                    direction: 'center',
                    className: 'circleLabel',
                })
                .addTo(mapRef.current!)
            return marker
        })

        const group = L.featureGroup(markers)
        mapRef.current.fitBounds(group.getBounds())

        return () => markers.forEach((marker) => marker.remove())
    }, [tasks, mapInfo])

    return (
        <div className="map-root">
            {mapInfo && (
                <StyledElements>
                    <LeafletTooltipStyles />
                    <StyledMapContainer whenReady={() => setMapReady(true)} ref={mapRef} attributionControl={false}>
                        <AuthTileLayer mapInfo={mapInfo} />
                    </StyledMapContainer>
                    <MapCompass />
                </StyledElements>
            )}
        </div>
    )
}
