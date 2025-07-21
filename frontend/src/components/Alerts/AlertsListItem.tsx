import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { Mission } from 'models/Mission'
import { TaskStatus } from 'models/Task'
import { ReactNode } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AlertCategory } from './AlertsBanner'

const StyledListContainer = styled.div`
    width: 330px;
    border-radius: 6px;
    border: 1px solid lightgray;
    display: flex;
    flex-direction: column;
    gap: 0;
`

const StyledListHeading = styled.div`
    display: flex;
    align-items: center;
    gap: 4px;
    padding: 8px 8px 0 8px;
    height: 30px;
`

const StyledListItem = styled.div`
    display: flex;
    flex-direction: column;
    gap: 10px;
    border-radius: 0 0 3px 3px;
    padding: 10px;
`

const StyledIcon = styled(Icon)`
    width: 24px;
    height: 24px;
    flex-shrink: 0;
`

const Horizontal = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
`

const Right = styled.div`
    display: flex;
    justify-content: flex-end;
    align-items: center;
`

const StyledButton = styled(Button)`
    margin-top: 9px;
    position: absolute;
    right: 20px;
    height: 30px;
    width: 30px;
    color: black;
`

interface AlertListInfo {
    icon: Icons
    alertTitle: string
    alertText: string
    iconColor?: string
    mission?: Mission
}

interface AlertProps {
    children: ReactNode
    dismissAlert: () => void
    alertCategory: AlertCategory
}

export const AlertListContents = ({ icon, iconColor, alertTitle, alertText, mission }: AlertListInfo) => {
    let missionHasFailedTasks = false
    if (mission !== undefined)
        missionHasFailedTasks = mission.tasks.some(
            (t) => t.status !== TaskStatus.PartiallySuccessful && t.status !== TaskStatus.Successful
        )
    return (
        <StyledListContainer>
            <StyledListHeading>
                <StyledIcon name={icon} style={{ color: iconColor }} />
                <Typography variant="h6">{alertTitle}</Typography>
            </StyledListHeading>
            <StyledListItem>
                <Typography variant="body_short">{alertText}</Typography>
                <Right>
                    {mission !== undefined && (
                        <MissionRestartButton
                            mission={mission}
                            hasFailedTasks={missionHasFailedTasks}
                            smallButton={false}
                        />
                    )}
                </Right>
            </StyledListItem>
        </StyledListContainer>
    )
}

export const AlertListItem = ({ children, dismissAlert }: AlertProps) => {
    return (
        <Horizontal>
            {children}
            <StyledButton variant="ghost_icon" onClick={dismissAlert}>
                <Icon name={Icons.Clear} size={18}></Icon>
            </StyledButton>
        </Horizontal>
    )
}
