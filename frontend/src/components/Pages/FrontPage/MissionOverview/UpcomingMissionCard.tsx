import { Button, Card, Checkbox, Dialog, Icon, Typography } from '@equinor/eds-core-react'
import { delete_forever } from '@equinor/eds-icons'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import { format, differenceInHours } from 'date-fns'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
interface UpcomingMissionCardProps {
    mission: Mission
    onDeleteMission: (mission: Mission) => void
}

interface MissionDisplayProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 900px;
    display: flex;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: auto 50px;
    align-items: center;
`
const HorizontalNonButtonContent = styled.div`
    display: grid;
    grid-template-columns: 50px 200px 100px auto auto auto;
    align-items: center;
`

const StyledMissionStartTime = styled.div`
    display: grid;
    grid-template-columns: auto auto auto;
    align-items: center;
`

const StyledConfirmDialog = styled.div`
    padding: 8px;
`

const StyledButtonSection = styled.div`
    display: grid;
    grid-template-columns: auto auto;
`

Icon.add({ delete_forever })

export function UpcomingMissionCard({ mission, onDeleteMission }: UpcomingMissionCardProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = '/robotics-frontend/mission/' + mission.id
        navigate(path)
    }
    const [confirmDeleteDialogOpen, setConfirmDeleteDialogOpen] = useState<boolean>(false)
    var numberOfTasks = 0
    mission.plannedTasks.forEach((task) => (numberOfTasks += task.inspections.length))
    return (
        <StyledMissionCard key={mission.id} variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <HorizontalNonButtonContent onClick={routeChange}>
                    <Checkbox />
                    <Typography variant="h6">{mission.name}</Typography>
                    <Typography>{mission.robot.name}</Typography>
                    <MissionStartTimeDisplay mission={mission} />
                    <Typography>Tasks: {numberOfTasks}</Typography>
                    <MissionDurationDisplay mission={mission} />
                </HorizontalNonButtonContent>
                <Button
                    variant="ghost_icon"
                    color="danger"
                    onClick={() => {
                        setConfirmDeleteDialogOpen(true)
                    }}
                >
                    <Icon name="delete_forever" size={24} title="more action" />
                </Button>
                <Dialog open={confirmDeleteDialogOpen} isDismissable>
                    <StyledConfirmDialog>
                        <Typography variant="h5">Please confirm that you want to delete:</Typography>
                        <Typography bold>{mission.name}</Typography>
                        <StyledButtonSection>
                            <Button
                                onClick={() => {
                                    setConfirmDeleteDialogOpen(false)
                                }}
                                variant="outlined"
                                color="secondary"
                            >
                                {' '}
                                Cancel{' '}
                            </Button>
                            <Button
                                color="danger"
                                onClick={() => {
                                    onDeleteMission(mission)
                                    setConfirmDeleteDialogOpen(false)
                                }}
                            >
                                {' '}
                                Delete mission
                            </Button>
                        </StyledButtonSection>
                    </StyledConfirmDialog>
                </Dialog>
            </HorizontalContent>
        </StyledMissionCard>
    )
}

function MissionStartTimeDisplay({ mission }: MissionDisplayProps) {
    return (
        <StyledMissionStartTime>
            <Typography>Date: {format(new Date(mission.startTime), 'dd. MMM')}</Typography>
            <Typography>Time: {format(new Date(mission.startTime), 'hh:mm')}</Typography>
        </StyledMissionStartTime>
    )
}

function MissionDurationDisplay({ mission }: MissionDisplayProps) {
    if (mission.endTime) {
        return (
            <Typography>
                Estimated duration: {differenceInHours(new Date(mission.endTime), new Date(mission.startTime))} h
            </Typography>
        )
    }
    return <></>
}
