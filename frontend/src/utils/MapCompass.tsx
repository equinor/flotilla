import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { Icon } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const StyledCompass = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
`

export const MapCompass = (): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    return (
        <>
            <StyledCompass>
                <Typography variant="h4" color="danger">
                    {TranslateText('N')}
                </Typography>
                <Icon
                    name={Icons.Navigation}
                    style={{ color: tokens.colors.infographic.primary__energy_red_100.rgba }}
                    size={32}
                />
                <Icon
                    name={Icons.Navigation}
                    style={{ color: tokens.colors.text.static_icons__default.rgba }}
                    size={32}
                    rotation={180}
                />
            </StyledCompass>
        </>
    )
}
