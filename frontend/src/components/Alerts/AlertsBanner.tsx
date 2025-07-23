import { Button, Icon } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { ReactNode, useState } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { phone_width } from 'utils/constants'

const StyledCard = styled.div`
    display: flex;
    width: 100vw - 10px;
    height: auto;
    min-height: 65px;
    padding: 6px 15px 2px 32px;
    justify-content: space-between;
    align-items: center;
    overflow: hidden;

    @media (max-width: ${phone_width}) {
        padding: 6px 8px 2px 10px;
    }
`
const Horizontal = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: center;
`
const Center = styled.div`
    align-items: center;
`

export enum AlertCategory {
    ERROR,
    WARNING,
    INFO,
}

interface AlertProps {
    children: ReactNode
    dismissAlert: () => void
    alertCategory: AlertCategory
}

export const AlertBanner = ({ children, dismissAlert, alertCategory }: AlertProps) => {
    let bannerColor = tokens.colors.ui.background__danger.hex
    const hoverColor = tokens.colors.ui.background__light.hex

    if (alertCategory === AlertCategory.WARNING) bannerColor = tokens.colors.interactive.warning__highlight.hex
    if (alertCategory === AlertCategory.INFO) bannerColor = tokens.colors.infographic.primary__mist_blue.hex

    const [buttonBackgroundColor, setButtonBackgroundColor] = useState<string>(bannerColor)

    return (
        <StyledCard style={{ backgroundColor: bannerColor }}>
            <Horizontal>
                <Center>{children}</Center>
            </Horizontal>
            <Button
                variant="ghost_icon"
                onClick={dismissAlert}
                style={{ backgroundColor: buttonBackgroundColor }}
                onPointerEnter={() => setButtonBackgroundColor(hoverColor)}
                onPointerLeave={() => setButtonBackgroundColor(bannerColor)}
            >
                <Icon name={Icons.Clear} style={{ color: tokens.colors.text.static_icons__default.hex }}></Icon>
            </Button>
        </StyledCard>
    )
}
