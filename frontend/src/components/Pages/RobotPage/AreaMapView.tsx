import { CircularProgress, Typography } from '@equinor/eds-core-react'
import { MouseEvent, useCallback, useEffect, useState } from 'react'
import styled from 'styled-components'
import NoMap from 'mediaAssets/NoMap.png'
import { PlaceRobotInMap, InverseCalculatePixelPosition } from '../../../utils/MapMarkers'
import { BackendAPICaller } from 'api/ApiCaller'
import { MapMetadata } from 'models/MapMetadata'
import { Area } from 'models/Area'
import { Position } from 'models/Position'
import { Pose } from 'models/Pose'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { MapCompass } from 'utils/MapCompass'

interface AreaProps {
    area: Area
    localizationPose: Pose
    setLocalizationPose: (newPose: Pose) => void
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

export function AreaMapView({ area, localizationPose, setLocalizationPose }: AreaProps) {
    const { TranslateText } = useLanguageContext()
    const [mapCanvas, setMapCanvas] = useState<HTMLCanvasElement>(document.createElement('canvas'))
    const [mapImage, setMapImage] = useState<HTMLImageElement>(document.createElement('img'))
    const [mapContext, setMapContext] = useState<CanvasRenderingContext2D>()
    const [mapMetadata, setMapMetadata] = useState<MapMetadata>()
    const [imageObjectURL, setImageObjectURL] = useState<string>()
    const [isLoading, setIsLoading] = useState<boolean>()

    const updateMap = useCallback(() => {
        let context = mapCanvas.getContext('2d')
        if (context === null) {
            return
        }
        context.clearRect(0, 0, mapCanvas.width, mapCanvas.height)
        context?.drawImage(mapImage, 0, 0)
        if (mapMetadata) {
            PlaceRobotInMap(mapMetadata, mapCanvas, localizationPose)
        }
    }, [mapCanvas, mapImage, mapMetadata, localizationPose])

    const getMeta = async (url: string) => {
        const image = new Image()
        image.src = url
        await image.decode()
        return image
    }

    useEffect(() => {
        setIsLoading(true)
        setImageObjectURL(undefined)
        BackendAPICaller.getAreasMapMetadata(area.id)
            .then((mapMetadata) => {
                setMapMetadata(mapMetadata)
                BackendAPICaller.getMap(area.installationCode, mapMetadata.mapName)
                    .then((imageBlob) => {
                        setImageObjectURL(URL.createObjectURL(imageBlob))
                    })
                    .catch(() => {
                        setImageObjectURL(NoMap)
                    })
            })
            .catch(() => {
                setMapMetadata(undefined)
                setImageObjectURL(NoMap)
            })
        //.catch((e) => handleError(e))
    }, [area])

    useEffect(() => {
        if (!imageObjectURL) {
            return
        }
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
        setIsLoading(false)
    }, [imageObjectURL])

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

    const onClickMap = (event: MouseEvent): void => {
        var rect = mapCanvas.getBoundingClientRect(),
            scaleX = mapCanvas.width / rect.width,
            scaleY = mapCanvas.height / rect.height
        const pixelPosition: Position = {
            x: (event.clientX - rect.left) * scaleX,
            y: (rect.bottom - event.clientY) * scaleY,
            z: 0,
        }
        if (!mapMetadata) {
            return
        }
        const assetPosition = InverseCalculatePixelPosition(mapMetadata, pixelPosition)
        let newPose: Pose = area.defaultLocalizationPose
        newPose.position.x = assetPosition[0]
        newPose.position.y = assetPosition[1]
        setLocalizationPose(newPose)
    }

    return (
        <>
            {isLoading && (
                <StyledLoading>
                    <CircularProgress />
                </StyledLoading>
            )}
            {!isLoading && (
                <>
                    {mapMetadata && (
                        <Typography variant="body_short_italic">
                            {TranslateText('Click on the map to move the localization position')}
                        </Typography>
                    )}
                    <StyledMapLimits>
                        <StyledMapCompass>
                            <StyledMap id="mapCanvas" onClick={onClickMap} />
                            {mapMetadata && <MapCompass />}
                        </StyledMapCompass>
                    </StyledMapLimits>
                </>
            )}
        </>
    )
}
