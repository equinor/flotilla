import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { MissionRestartButton } from 'components/Displays/MissionButtons/MissionRestartButton'
import { Mission } from 'models/Mission'
import { TaskStatus, TaskType } from 'models/Task'
import { ReactNode } from 'react'
import styled from 'styled-components'
import { Icons } from 'utils/icons'
import { AlertCategory } from './AlertsBanner'

const StyledListContainer = styled.div`
    isplay: flex;
    width: 300px;
    height: auto;
    justify-content: space-between;
    align-items: center;
    padding: 3px 10px 2px 10px;
`

const outline = styled.div`
    outline: solid;
    outline-width: 1px;
    outline-color: lightgray;
    padding: 6px 10px 6px 10px;
`

const StyledListHeading = styled(outline)`
    display: flex;
    justify-content: space-between;
    padding: 6px 5px 6px 5px;
`
const StyledListItem = styled(outline)`
    display: flex;
    flex-direction: column;
    align-items: left;
    row-gap: 10px;
`

const VerticalContent = styled.div`
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
                <VerticalContent>
                    <StyledIcon name={icon} style={{ color: iconColor }} />
                    <Typography variant="h6">{alertTitle}</Typography>
                </VerticalContent>
            </StyledListHeading>
            <StyledListItem>
                <Typography variant="caption">{alertText}</Typography>
                <Right>
                    {mission !== undefined &&
                        mission.tasks[0]?.type !== TaskType.ReturnHome &&
                        mission.tasks[0]?.type !== TaskType.Localization && (
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
            <Button
                variant="ghost_icon"
                onClick={dismissAlert}
                style={{ marginTop: '0px', right: '25px', position: 'absolute' }}
            >
                <Icon name={Icons.Clear}></Icon>
            </Button>
        </Horizontal>
    )
}
