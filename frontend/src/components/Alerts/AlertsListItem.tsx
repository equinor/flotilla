import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { Mission } from 'models/Mission'
import { TaskStatus } from 'models/Task'
import { ReactNode } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AlertCategory } from './AlertsBanner'

const StyledListContainer = styled.div`
    isplay: flex;
    width: 320px;
    height: auto;
    justify-content: space-between;
    align-items: center;
`
const outline = styled.div`
    border: 1px solid lightgray;
`
const StyledListHeading = styled(outline)`
    display: flex;
    height: 40px;
    align-items: center;
    justify-content: space-between;
    padding: 6px 5px 6px 5px;
    border-radius: 3px 3px 0px 0px;
`
const StyledListItem = styled(outline)`
    display: flex;
    flex-direction: column;
    align-items: left;
    row-gap: 10px;
    border-radius: 0px 0px 3px 3px;
    border-top: none;
    padding: 5px;
`
const HorizontalContent = styled.div`
    display: flex;
    flex-direction: row;
    align-items: center;
    column-gap: 4px;
`
const StyledIcon = styled(Icon)`
    width: 24px;
    min-width: 24px;
    height: 24px;
`
const Horizontal = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    align-items: top;
`
const Right = styled.div`
    display: flex;
    align-items: right;
    justify-content: right;
    align-content: center;
`
const StyledButton = styled(Button)`
    margin-top: 15px;
    right: 26px;
    position: absolute;
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
                <HorizontalContent>
                    <StyledIcon name={icon} style={{ color: iconColor }} />
                    <Typography variant="h6">{alertTitle}</Typography>
                </HorizontalContent>
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
