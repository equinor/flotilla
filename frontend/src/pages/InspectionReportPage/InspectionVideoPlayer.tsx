import ReactPlayer from 'react-player'
import { Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { phone_width, tablet_width } from 'utils/constants'

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

const StyledVideoPlayer = styled(ReactPlayer)`
    width: auto !important;
    height: auto !important;
    max-width: 100%;
    max-height: calc(85vh - 174px);
    @media (max-width: ${tablet_width}) {
        max-height: calc(80vh - 174px);
    }
    @media (max-width: ${phone_width}) {
        max-height: calc(70vh - 174px);
    }
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
        <StyledVideoPlayer src={src} controls playsInline />
    </StyledVideoWrapper>
)
