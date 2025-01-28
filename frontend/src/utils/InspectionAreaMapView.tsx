import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { placeRobotInMap } from 'utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { MapMetadata } from 'models/MapMetadata'
import { Pose } from 'models/Pose'
import { MapCompass } from 'utils/MapCompass'
import { InspectionArea } from 'models/InspectionArea'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { getMeta } from 'components/Pages/MissionPage/MapPosition/MissionMapView'

interface InspectionAreaProps {
    inspectionArea: InspectionArea
    markedRobotPosition: Pose
}

const StyledMap = styled.canvas`
    max-height: 50vh;
    max-width: 90%;
    margin: auto;
`
const StyledMapLimits = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
`
const StyledLoading = styled.div`
    display: flex;
    justify-content: center;
`
const StyledMapCompass = styled.div`
    display: flex;
    flex-direction: row;
    align-items: end;
`
const StyledCaption = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: left;
    gap: 10px;
`

export const InspectionAreaMapView = ({ inspectionArea, markedRobotPosition }: InspectionAreaProps) => {
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [mapMetadata, setMapMetadata] = useState<MapMetadata>()
    const [isLoading, setIsLoading] = useState<boolean>()
    const { TranslateText } = useLanguageContext()

    const updateMap = useCallback(() => {
        const context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        if (mapMetadata) {
            placeRobotInMap(mapMetadata, mapCanvas, markedRobotPosition)
        }
    }, [mapCanvas, mapImage, mapMetadata, markedRobotPosition])

    let mapName = mapMetadata?.mapName.split('.')[0].replace(/[^0-9a-zA-Z ]/g, ' ')
    mapName = mapName ? mapName.charAt(0).toUpperCase() + mapName.slice(1) : ' '

    useEffect(() => {
        const processImageURL = (imageBlob: Blob | string) => {
            const imageObjectURL = typeof imageBlob === 'string' ? imageBlob : URL.createObjectURL(imageBlob as Blob)
            if (!imageObjectURL) return

            getMeta(imageObjectURL as string)
                .then((img) => {
                    const mapCanvas = document.getElementById('inspectionAreaMapCanvas') as HTMLCanvasElement
                    if (!mapCanvas) return
                    mapCanvas.width = img.width
                    mapCanvas.height = img.height
                    const context = mapCanvas?.getContext('2d')
                    if (context) {
                        setMapContext(context)
                        context.drawImage(img, 0, 0)
                    }
                    setMapCanvas(mapCanvas)
                    setMapImage(img)
                })
                .catch((error) => {
                    console.error('Failed to get image metadata:', error)
                })
            setIsLoading(false)
        }

        setIsLoading(true)
        BackendAPICaller.getInspectionAreaMapMetadata(inspectionArea.id)
            .then((mapMetadata) => {
                setMapMetadata(mapMetadata)
                BackendAPICaller.getMap(inspectionArea.installationCode, mapMetadata.mapName)
                    .then(processImageURL)
                    .catch(() => {
                        setIsLoading(false)
                        processImageURL(NoMap)
                    })
            })
            .catch(() => {
                setMapMetadata(undefined)
                processImageURL(NoMap)
            })
    }, [])

    useEffect(() => {
        let animationFrameId = 0
        if (mapContext) {
            const render = () => {
                updateMap()
                animationFrameId = window.requestAnimationFrame(render)
            }
            render()
        }
        return () => window.cancelAnimationFrame(animationFrameId)
    }, [updateMap, mapContext])

    return (
        <>
            {isLoading && (
                <StyledLoading>
                    <CircularProgress />
                </StyledLoading>
            )}
            {!isLoading && (
                <StyledMapLimits>
                    <StyledCaption>
                        <StyledMapCompass>
                            <StyledMap id="inspectionAreaMapCanvas" />
                            {mapMetadata && <MapCompass />}
                        </StyledMapCompass>
                        {mapMetadata !== undefined && (
                            <Typography italic variant="body_short">
                                {TranslateText('Map of {0}', [mapName])}
                            </Typography>
                        )}
                    </StyledCaption>
                </StyledMapLimits>
            )}
        </>
    )
}
