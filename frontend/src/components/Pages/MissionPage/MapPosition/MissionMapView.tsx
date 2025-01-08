import { Card, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { useCallback, useEffect, useRef, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { placeRobotInMap, placeTagsInMap } from 'utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { TaskStatus } from 'models/Task'
import { MapCompass } from 'utils/MapCompass'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { useQuery } from '@tanstack/react-query'

interface MissionProps {
    mission: Mission
}

const MapCard = styled(Card)`
    display: flex;
    max-width: 600px;
    padding: 16px;
    justify-items: center;
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
    const { enabledRobots } = useRobotContext()
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [currentTaskOrder, setCurrentTaskOrder] = useState<number>(0)

    const missionRobot = enabledRobots.find((robot) => robot.id === mission.robot.id)

    const imageObjectURL = useRef<string>('')

    const fetchMapInfo = (mission: Mission) => {
        const map = useQuery({
            queryKey: [mission.missionId],
            queryFn: async () => {
                const missionDefinition = await BackendAPICaller.getMissionDefinitionById(mission.missionId!)
                return missionDefinition.map
            },
            retryDelay: 60 * 1000,
            staleTime: 60 * 1000,
            enabled: mission.missionId !== undefined,
        })
        return map.data
    }

    const mapInfo = fetchMapInfo(mission)

    const updateMap = useCallback(() => {
        const context = mapCanvas.getContext('2d')
        if (!context || !mapInfo) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context.drawImage(mapImage, 0, 0)
        placeTagsInMap(mission.tasks, mapInfo, mapCanvas, currentTaskOrder)
        if (missionRobot?.pose && mapInfo) {
            placeRobotInMap(mapInfo, mapCanvas, missionRobot.pose)
        }
    }, [currentTaskOrder, mapCanvas, mapImage, mission, missionRobot?.pose])

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    const findCurrentTaskOrder = useCallback(
        () =>
            mission.tasks
                .filter((task) => task.status === TaskStatus.InProgress || task.status === TaskStatus.Paused)
                .map((task) => task.taskOrder)[0],
        [mission.tasks]
    )

    let displayedMapName = mapInfo?.mapName.split('.')[0].replace(/[^0-9a-z-A-Z ]/g, ' ')
    displayedMapName = displayedMapName ? displayedMapName.charAt(0).toUpperCase() + displayedMapName.slice(1) : ' '

    useEffect(() => {
        if (mapInfo?.mapName) {
            BackendAPICaller.getMap(mission.installationCode!, mapInfo.mapName)
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
                            const context = mapCanvas?.getContext('2d')
                            if (context) {
                                setMapContext(context)
                                context.drawImage(img, 0, 0)
                            }
                            setMapCanvas(mapCanvas)
                        }
                        setMapImage(img)
                    })
                })
        } else {
            imageObjectURL.current = NoMap
        }
    }, [mission.installationCode, mission.id, mapInfo?.mapName])

    useEffect(() => {
        if (mission.isCompleted) {
            const maxTaskOrder: number = Math.max(...mission.tasks.map((task) => task.taskOrder))
            setCurrentTaskOrder(maxTaskOrder + 1)
        } else {
            setCurrentTaskOrder(findCurrentTaskOrder())
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
            <Typography variant="h3">{displayedMapName}</Typography>
            <SyledContainer>
                <StyledElements>
                    <StyledMap id="mapCanvas" />
                    {imageObjectURL.current !== NoMap && mapContext && <MapCompass />}
                </StyledElements>
            </SyledContainer>
        </MapCard>
    )
}
