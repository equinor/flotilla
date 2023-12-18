import { Card } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { useCallback, useEffect, useRef, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { placeRobotInMap, placeTagsInMap } from 'utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { TaskStatus } from 'models/Task'
import { MapCompass } from 'utils/MapCompass'

interface MissionProps {
    mission: Mission
}

const MapCard = styled(Card)`
    display: flex;
    max-width: 600px;
    padding: 16px;
`

const StyledMap = styled.canvas`
    object-fit: contain;
    max-height: 100%;
    max-width: 90%;
    margin: auto;
`

const StyledElements = styled.div`
    display: flex;
    flex-direction: columns;
    align-items: end;
`

const SyledContainer = styled.div`
    display: flex;
    max-height: 600px;
    max-width: 100%;
`

export const MissionMapView = ({ mission }: MissionProps) => {
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [currentTaskOrder, setCurrentTaskOrder] = useState<number>()

    const imageObjectURL = useRef<string>('')

    const updateMap = useCallback(() => {
        let context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        placeTagsInMap(mission, mapCanvas, currentTaskOrder)
        if (mission.robot.pose && mission.map) {
            placeRobotInMap(mission.map, mapCanvas, mission.robot.pose)
        }
    }, [currentTaskOrder, mapCanvas, mapImage, mission])

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    const findCurrentTaskOrder = useCallback(() => {
        mission.tasks.forEach((task) => {
            if (task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused) {
                setCurrentTaskOrder(task.taskOrder)
            }
        })
    }, [mission.tasks])

    useEffect(() => {
        BackendAPICaller.getMap(mission.installationCode!, mission.map?.mapName!)
            .then((imageBlob) => {
                imageObjectURL.current = URL.createObjectURL(imageBlob)
            })
            .catch(() => {
                imageObjectURL.current = NoMap
            })
            .then(() => {
                getMeta(imageObjectURL.current).then((img) => {
                    const mapCanvas = document.getElementById('mapCanvas') as HTMLCanvasElement
                    if (mapCanvas) {
                        mapCanvas.width = img.width
                        mapCanvas.height = img.height
                        let context = mapCanvas?.getContext('2d')
                        if (context) {
                            setMapContext(context)
                            context.drawImage(img, 0, 0)
                        }
                        setMapCanvas(mapCanvas)
                    }
                    setMapImage(img)
                })
            })
    }, [mission.installationCode, mission.id, mission.map?.mapName])

    useEffect(() => {
        if (mission.isCompleted) {
            setCurrentTaskOrder(undefined)
        } else {
            findCurrentTaskOrder()
        }
    }, [findCurrentTaskOrder, mission])

    useEffect(() => {
        let animationFrameId = 0
        if (mapContext) {
            const render = () => {
                updateMap()
                animationFrameId = window.requestAnimationFrame(render)
            }
            render()
        }
        return () => {
            window.cancelAnimationFrame(animationFrameId)
        }
    }, [updateMap, mapContext])

    return (
        <MapCard style={{ boxShadow: tokens.elevation.raised }}>
            <SyledContainer>
                <StyledElements>
                    <StyledMap id="mapCanvas" />
                    {imageObjectURL.current !== NoMap && mapContext && <MapCompass />}
                </StyledElements>
            </SyledContainer>
        </MapCard>
    )
}
