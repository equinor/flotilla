import { Button, Icon } from '@equinor/eds-core-react'
import { ReactNode } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'

const StyledListItem = styled.div`
    display: flex;
    width: 300px;
    height: auto;
    padding: 3px 10px 2px 10px;
    justify-content: space-between;
    align-items: center;
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

export const AlertListItem = ({ children, dismissAlert, alertCategory }: AlertProps) => {
    return (
        <>
            <StyledListItem>
                <Horizontal>
                    <Center>{children}</Center>
                </Horizontal>
                <Button variant="ghost_icon" onClick={dismissAlert}>
                    <Icon name={Icons.Clear} size={24}></Icon>
                </Button>
            </StyledListItem>
        </>
    )
}
