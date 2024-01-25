import { CircularProgress } from '@equinor/eds-core-react'
import { useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { placeRobotInMap } from 'utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { MapMetadata } from 'models/MapMetadata'
import { Pose } from 'models/Pose'
import { MapCompass } from 'utils/MapCompass'
import { Deck } from 'models/Deck'

interface DeckProps {
    deck: Deck
    markedRobotPosition: Pose
}

const StyledMap = styled.canvas`
    object-fit: contain;
    max-height: 100%;
    max-width: 90%;
    margin: auto;
`

const StyledMapLimits = styled.div`
    display: flex;
    max-height: 600px;
    max-width: 600px;
    padding-left: 30px;
    justify-content: center;
`

const StyledLoading = styled.div`
    display: flex;
    justify-content: center;
`

const StyledMapCompass = styled.div`
    display: flex;
    flex-direction: columns;
    align-items: end;
`

export const DeckMapView = ({ deck, markedRobotPosition }: DeckProps) => {
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [mapMetadata, setMapMetadata] = useState<MapMetadata>()
    const [isLoading, setIsLoading] = useState<boolean>()

    const updateMap = useCallback(() => {
        let context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        if (mapMetadata) {
            placeRobotInMap(mapMetadata, mapCanvas, markedRobotPosition)
        }
    }, [mapCanvas, mapImage, mapMetadata, markedRobotPosition])

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    useEffect(() => {
        const processImageURL = (imageBlob: Blob | string) => {
            const imageObjectURL = typeof imageBlob === 'string' ? imageBlob : URL.createObjectURL(imageBlob as Blob)
            if (!imageObjectURL) return

            getMeta(imageObjectURL as string).then((img) => {
                const mapCanvas = document.getElementById('deckMapCanvas') as HTMLCanvasElement
                if (!mapCanvas) return
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
            setIsLoading(false)
        }

        setIsLoading(true)
        BackendAPICaller.getDeckMapMetadata(deck.id)
            .then((mapMetadata) => {
                setMapMetadata(mapMetadata)
                BackendAPICaller.getMap(deck.installationCode, mapMetadata.mapName)
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
        // eslint-disable-next-line react-hooks/exhaustive-deps
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
                    <StyledMapCompass>
                        <StyledMap id="deckMapCanvas" />
                        {mapMetadata && <MapCompass />}
                    </StyledMapCompass>
                </StyledMapLimits>
            )}
        </>
    )
}
