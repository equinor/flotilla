import { Button, Card, Checkbox, Dialog, Icon, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Text } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

interface MissionQueueCardProps {
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
    grid-template-columns: 50px 400px auto 90px 180px;
    align-items: center;
`

const StyledConfirmDialog = styled.div`
    padding: 8px;
`

const StyledButtonSection = styled.div`
    display: grid;
    grid-template-columns: auto auto;
`

export function MissionQueueCard({ mission, onDeleteMission }: MissionQueueCardProps) {
    let navigate = useNavigate()
    const routeChange = () => {
        let path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    const [confirmDeleteDialogOpen, setConfirmDeleteDialogOpen] = useState<boolean>(false)
    var numberOfTasks = 0
    mission.tasks.forEach((task) => (numberOfTasks += task.inspections.length))
    return (
        <StyledMissionCard key={mission.id} variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <HorizontalNonButtonContent onClick={routeChange}>
                    <Checkbox />
                    <Button variant="ghost" fullWidth>
                        <Typography variant="body_short_bold">{mission.name}</Typography>
                    </Button>
                    <Typography variant="caption" color="#6F6F6F">
                        {Text('Robot')}: {mission.robot.name}
                    </Typography>
                    <Typography variant="caption" color="#6F6F6F">
                        {Text('Tasks')}: {numberOfTasks}
                    </Typography>
                    <MissionDurationDisplay mission={mission} />
                </HorizontalNonButtonContent>
                <Button
                    variant="ghost_icon"
                    onClick={() => {
                        setConfirmDeleteDialogOpen(true)
                    }}
                >
                    <Icon name={Icons.Remove} size={24} title="more action" />
                </Button>
                <Dialog open={confirmDeleteDialogOpen} isDismissable>
                    <StyledConfirmDialog>
                        <Typography variant="h5">
                            {Text('Please confirm that you want to remove the mission from the queue:')}
                        </Typography>
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
                                {Text('Cancel')}{' '}
                            </Button>
                            <Button
                                color="danger"
                                onClick={() => {
                                    onDeleteMission(mission)
                                    setConfirmDeleteDialogOpen(false)
                                }}
                            >
                                {' '}
                                {Text('Remove mission')}
                            </Button>
                        </StyledButtonSection>
                    </StyledConfirmDialog>
                </Dialog>
            </HorizontalContent>
        </StyledMissionCard>
    )
}

function MissionDurationDisplay({ mission }: MissionDisplayProps) {
    if (mission.estimatedDuration) {
        const hours = Math.floor(mission.estimatedDuration / 3600)
        const remainingSeconds = mission.estimatedDuration % 3600
        const minutes = Math.ceil(remainingSeconds / 60)
        return (
            <Typography variant="caption" color="#6F6F6F">
                {Text('Estimated duration')}: {hours}
                {Text('h')} {minutes}
                {Text('min')}
            </Typography>
        )
    }
    return (
        <Typography variant="caption" color="#6F6F6F">
            {Text('Estimated duration: not available')}
        </Typography>
    )
}
