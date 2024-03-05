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
`

const Center = styled.div`
    align-items: center;
`

interface AlertProps {
    children: ReactNode
    dismissAlert: () => void
}

export const AlertBanner = ({ children, dismissAlert }: AlertProps) => {
    return (
        <>
            <StyledCard variant="danger" style={{ boxShadow: tokens.elevation.raised }}>
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
