import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { phone_width } from 'utils/constants'
import { Alert } from 'models/Alert'

const StyledCard = styled.div`
    display: flex;
    width: 100%;
    box-sizing: border-box;
    height: auto;
    padding: 0.5rem 1.5rem;
    justify-content: space-between;
    align-items: center;
    overflow: hidden;

    @media (max-width: ${phone_width}) {
        padding: 0.5rem 1rem;
    }
`
const Horizontal = styled.div`
    display: flex;
    flex-direction: row;
    gap: 1rem;
    align-items: center;
`
const DismissButton = styled(Button)`
    &:hover {
        background-color: ${tokens.colors.ui.background__light.hex};
    }
`

interface Props {
    bannerAlert: Alert
    dismissAlert: () => void
}

export const AlertBanner = ({ bannerAlert, dismissAlert }: Props) => {
    let bannerColor = tokens.colors.ui.background__danger.hex
    let iconColor = tokens.colors.interactive.danger__resting.hex
    if (bannerAlert.severity === 'warning') {
        bannerColor = tokens.colors.interactive.warning__highlight.hex
        iconColor = tokens.colors.interactive.warning__resting.hex
    }
    if (bannerAlert.severity === 'info') {
        bannerColor = tokens.colors.infographic.primary__mist_blue.hex
        iconColor = tokens.colors.text.static_icons__default.hex
    }

    return (
        <StyledCard style={{ backgroundColor: bannerColor }}>
            <Horizontal>
                <Icon name={Icons.Failed} style={{ color: iconColor }} />
                <div>
                    {bannerAlert.title && <Typography variant="h4">{bannerAlert.title}</Typography>}
                    {bannerAlert.message && <Typography variant="body_short">{bannerAlert.message}</Typography>}
                </div>
            </Horizontal>
            <DismissButton variant="ghost_icon" onClick={dismissAlert}>
                <Icon name={Icons.Clear}></Icon>
            </DismissButton>
        </StyledCard>
    )
}
