import { Card } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { useEffect, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { useErrorHandler } from 'react-error-boundary'
import { defaultPose, Pose } from 'models/Pose'
import { PlaceRobotInMap, PlaceTagsInMap } from './MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { TaskStatus } from 'models/Task'

interface MissionProps {
    mission: Mission
}

const MapCard = styled(Card)`
    display: flex;
    height: 600px;
    width: 600px;
    padding: 16px;
`

const StyledMap = styled.canvas`
    object-fit: contain;
    max-height: 100%;
    max-width: 100%;
    margin: auto;
`

export function MapView({ mission }: MissionProps) {
    const handleError = useErrorHandler()
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [previousRobotPose, setPreviousRobotPose] = useState<Pose>(defaultPose)
    const [currentRobotPose, setCurrentRobotPose] = useState<Pose>(defaultPose)
    const [currentTaskOrder, setCurrentTaskOrder] = useState<number>()

    let imageObjectURL: string

    const updateMap = () => {
        let context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        PlaceTagsInMap(mission, mapCanvas, currentTaskOrder)
        PlaceRobotInMap(mission, mapCanvas, currentRobotPose, previousRobotPose)
    }

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    const findCurrentTaskOrder = () => {
        mission.tasks.map(function (task) {
            if (task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused) {
                setCurrentTaskOrder(task.taskOrder)
            }
        })
    }

    useEffect(() => {
        BackendAPICaller.getMap(mission.assetCode!, mission.map?.mapName!)
            .then((imageBlob) => {
                imageObjectURL = URL.createObjectURL(imageBlob)
            })
            .catch(() => {
                imageObjectURL = NoMap
            })
            .then(() => {
                getMeta(imageObjectURL).then((img) => {
                    const mapCanvas = document.getElementById('mapCanvas') as HTMLCanvasElement
                    mapCanvas.width = img.width
                    mapCanvas.height = img.height
                    let context = mapCanvas?.getContext('2d')
                    if (context) {
                        setMapContext(context)
                        context.drawImage(img, 0, 0)
                    }
                    setMapCanvas(mapCanvas)
                    setMapImage(img)
                })
            })
        //.catch((e) => handleError(e))
    }, [])

    useEffect(() => {
        if (mission.robot.pose) {
            setPreviousRobotPose(currentRobotPose)
            setCurrentRobotPose(mission.robot.pose)
        }
    }, [mission.robot.pose])

    useEffect(() => {
        if (mission.isCompleted) {
            setCurrentTaskOrder(undefined)
        } else {
            findCurrentTaskOrder()
        }
    }, [mission])

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
        <MapCard variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <StyledMap id="mapCanvas" />
        </MapCard>
    )
}
