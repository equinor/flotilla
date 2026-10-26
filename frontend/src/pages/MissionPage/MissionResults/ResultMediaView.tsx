import { Icon, Typography } from '@equinor/eds-core-react'
import { play_circle } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ReactNode, useState } from 'react'
import styled from 'styled-components'
import { VideoPlaceholder, VideoPlayer } from 'pages/InspectionReportPage/InspectionVideoPlayer'
import { StyledInspectionImage } from 'pages/InspectionReportPage/InspectionStyles'
import { FileType } from 'models/InspectionRecord'

type MediaSize = 'preview' | 'analysis' | 'thumbnail' | 'large'

const spacing = tokens.spacings.comfortable

const Frame = styled.div<{ $size: MediaSize }>`
    position: relative;
    display: flex;
    align-items: center;
    justify-content: center;
    width: 100%;
    --media-height: ${({ $size }) =>
        ({ large: 'min(48vh, 520px)', thumbnail: '64px', analysis: '112px', preview: '160px' })[$size]};
    height: var(--media-height);
    min-height: ${({ $size }) => ($size === 'thumbnail' ? '64px' : '96px')};
    overflow: hidden;
    video {
        max-height: var(--media-height);
        max-width: 100%;
    }
`

const ImageSurface = styled.div<{ $aspectRatio: number | undefined }>`
    position: relative;
    width: ${({ $aspectRatio }) => ($aspectRatio ? `min(100%, calc(var(--media-height) * ${$aspectRatio}))` : '100%')};
    overflow: hidden;
    img {
        display: block;
        width: 100%;
        height: auto;
        object-fit: contain;
    }
`

const InspectionTypeLabel = styled(Typography)<{ $aboveControls: boolean }>`
    --label-inset: 10px;
    position: absolute;
    left: var(--label-inset);
    ${({ $aboveControls }) => ($aboveControls ? 'top: var(--label-inset);' : 'bottom: var(--label-inset);')}
    max-width: calc(100% - 2 * var(--label-inset));
    box-sizing: border-box;
    padding: ${spacing.x_small} ${spacing.small};
    border-radius: 2px;
    background: ${tokens.colors.text.static_icons__default.hex}e6;
    color: ${tokens.colors.ui.background__default.hex};
    font-weight: 400;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    pointer-events: none;
`

const LoadedImage = ({ source, label, overlay }: { source: string; label: string; overlay?: ReactNode }) => {
    const [failed, setFailed] = useState(false)
    const [aspectRatio, setAspectRatio] = useState<number>()
    const { TranslateText } = useLanguageContext()
    return failed ? (
        <Typography role="alert">{TranslateText('Failed to load image')}</Typography>
    ) : (
        <ImageSurface $aspectRatio={aspectRatio}>
            <StyledInspectionImage
                src={source}
                alt={label}
                onLoad={({ currentTarget }) => setAspectRatio(currentTarget.naturalWidth / currentTarget.naturalHeight)}
                onError={() => setFailed(true)}
            />
            {overlay}
        </ImageSurface>
    )
}

const LoadedVideo = ({ source }: { source: string }) => {
    const [failed, setFailed] = useState(false)
    const { TranslateText } = useLanguageContext()
    return failed ? (
        <Typography role="alert">{TranslateText('Failed to load video')}</Typography>
    ) : (
        <VideoPlayer src={source} onError={() => setFailed(true)} />
    )
}

export const ResultMediaView = ({
    source,
    label,
    size = 'preview',
    fileType,
}: {
    source: string
    label: string
    size?: MediaSize
    fileType: FileType
}) => {
    const { TranslateText } = useLanguageContext()
    const typeLabel = fileType && size !== 'thumbnail' && (
        <InspectionTypeLabel as="span" variant="caption" $aboveControls={size === 'large'}>
            {TranslateText(fileType)}
        </InspectionTypeLabel>
    )
    return (
        <Frame $size={size}>
            {fileType === FileType.IMAGE && <LoadedImage source={source} label={label} overlay={typeLabel} />}
            {fileType === FileType.VIDEO &&
                (size === 'large' ? (
                    <LoadedVideo source={source} />
                ) : size === 'thumbnail' ? (
                    <Icon data={play_circle} title={TranslateText('Video')} />
                ) : (
                    <VideoPlaceholder />
                ))}
            {fileType === FileType.VALUE && (
                <Typography role="alert">{TranslateText('Viewing of the inspection type is not supported')}</Typography>
            )}
            {fileType !== FileType.IMAGE && typeLabel}
        </Frame>
    )
}
