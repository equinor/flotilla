import ReactPlayer from 'react-player'
import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const StyledVideoPlaceholder = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    gap: 8px;
    padding: 16px 8px;
    height: 100%;
    width: 100%;
`

const StyledVideoWrapper = styled.div`
    display: flex;
    justify-content: center;
    align-items: center;
    width: 100%;
`

export const VideoPlaceholder = () => {
    const { TranslateText } = useLanguageContext()
    return (
        <StyledVideoPlaceholder>
            <Icon name={Icons.PlayButton} size={48} color={tokens.colors.text.static_icons__tertiary.hex} />
            <Typography color={tokens.colors.text.static_icons__tertiary.hex}>{TranslateText('Video')}</Typography>
        </StyledVideoPlaceholder>
    )
}

export const VideoPlayer = ({ src }: { src: string }) => (
    <StyledVideoWrapper>
        <ReactPlayer src={src} controls playsInline style={{ maxWidth: '100%', maxHeight: 'calc(80vh - 174px)' }} />
    </StyledVideoWrapper>
)
