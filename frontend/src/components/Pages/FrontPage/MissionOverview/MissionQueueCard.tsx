import { Button, Card, Checkbox, Chip, Dialog, Icon, Typography } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

interface MissionQueueCardProps {
    mission: Mission
    order: number
    onDeleteMission: (mission: Mission) => void
    onReorderMission: (mission: Mission, offset: number) => void
}

interface MissionDisplayProps {
    mission: Mission
}

const IconStyle = styled.div`
    display: flex;
    align-items: center;
    flex-direction: row-reverse;
`

const StyledMissionCard = styled(Card)`
    width: 1080px;
    display: flex;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: auto 50px;
    align-items: center;
`
const HorizontalNonButtonContent = styled.div`
    display: grid;
    grid-template-columns: 50px 20px 30px 30px 400px auto 90px 180px;
    align-items: center;
`

const StyledConfirmDialog = styled.div`
    padding: 8px;
`

const StyledButtonSection = styled.div`
    display: grid;
    grid-template-columns: auto auto;
`

export function MissionQueueCard({ mission, order, onDeleteMission, onReorderMission }: MissionQueueCardProps) {
    const { TranslateText } = useLanguageContext()
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
                <HorizontalNonButtonContent>
                    <Checkbox />
                        <Chip variant="active">
                            <Typography variant="caption" color="#6F6F6F">
                                {order + 1}
                            </Typography>
                        </Chip>
                    <Button variant="ghost" onClick={() => onReorderMission(mission, -1)} >
                        <IconStyle>
                            <Icon name={Icons.ChevronUp} size={16} />
                        </IconStyle>
                    </Button>
                    <Button variant="ghost" onClick={() => onReorderMission(mission, 1)} >
                        <IconStyle>
                            <Icon name={Icons.ChevronDown} size={16} />
                        </IconStyle>
                    </Button>
                    <Button variant="ghost" fullWidth onClick={routeChange}>
                        <Typography variant="body_short_bold">{mission.name}</Typography>
                    </Button>
                    <Typography variant="caption" color="#6F6F6F" onClick={routeChange}>
                        {TranslateText('Robot')}: {mission.robot.name}
                    </Typography>
                    <Typography variant="caption" color="#6F6F6F" onClick={routeChange}>
                        {TranslateText('Tasks')}: {numberOfTasks}
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
                            {TranslateText('Please confirm that you want to remove the mission from the queue:')}
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
                                {TranslateText('Cancel')}{' '}
                            </Button>
                            <Button
                                onClick={() => {
                                    onDeleteMission(mission)
                                    setConfirmDeleteDialogOpen(false)
                                }}
                            >
                                {' '}
                                {TranslateText('Remove mission')}
                            </Button>
                        </StyledButtonSection>
                    </StyledConfirmDialog>
                </Dialog>
            </HorizontalContent>
        </StyledMissionCard>
    )
}

function MissionDurationDisplay({ mission }: MissionDisplayProps) {
    const { TranslateText } = useLanguageContext()
    const translateEstimatedDuration = TranslateText('Estimated duration')
    const translateH = TranslateText('h')
    const translateMin = TranslateText('min')
    const translateNotAvailable = TranslateText('Estimated duration: not available')

    if (mission.estimatedDuration) {
        const hours = Math.floor(mission.estimatedDuration / 3600)
        const remainingSeconds = mission.estimatedDuration % 3600
        const minutes = Math.ceil(remainingSeconds / 60)

        return (
            <Typography variant="caption" color="#6F6F6F">
                {translateEstimatedDuration}: {hours}
                {translateH} {minutes}
                {translateMin}
            </Typography>
        )
    }

    return (
        <Typography variant="caption" color="#6F6F6F">
            {translateNotAvailable}
        </Typography>
    )
}
