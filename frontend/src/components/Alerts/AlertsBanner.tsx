import { Button, Card, Icon } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { ReactNode } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const StyledCard = styled(Card)`
    display: flex;
    width: auto;
    padding: 7px 15px;
    margin: 0px 8px 8px 8px;
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
    SUCCESS,
}

interface AlertProps {
    children: ReactNode
    dismissAlert: () => void
    alertCategory: AlertCategory
}

export const AlertBanner = ({ children, dismissAlert, alertCategory }: AlertProps) => {
    let bannerColor = tokens.colors.ui.background__danger.hex

    if (alertCategory === AlertCategory.WARNING) bannerColor = tokens.colors.interactive.warning__resting.hex
    if (alertCategory === AlertCategory.SUCCESS) bannerColor = tokens.colors.interactive.success__resting.hex

    return (
        <>
            <StyledCard style={{ boxShadow: tokens.elevation.raised, backgroundColor: bannerColor }}>
                <Horizontal>
                    <Center>{children}</Center>
                    <Button variant="ghost_icon" onClick={dismissAlert}>
                        <Icon name={Icons.Clear}></Icon>
                    </Button>
                </Horizontal>
            </StyledCard>
        </>
    )
}
