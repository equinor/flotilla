import { Button, Card, Dialog, Icon, Typography, DotProgress } from '@equinor/eds-core-react'
import { config } from 'config'
import { tokens } from '@equinor/eds-tokens'
import { Mission, placeholderMission } from 'models/Mission'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'

interface MissionQueueCardProps {
    order: number
    mission: Mission
    onDeleteMission: (mission: Mission) => void
}

interface MissionDisplayProps {
    mission: Mission
}

const StyledMissionCard = styled(Card)`
    width: 880px;
    display: flex;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: auto 50px;
    align-items: center;
`
const HorizontalNonButtonContent = styled.div`
    display: grid;
    grid-template-columns: 20px 400px auto 90px 180px;
    align-items: center;
    padding-left: 10px;
`

const StyledConfirmDialog = styled.div`
    padding: 8px;
`

const StyledButtonSection = styled.div`
    display: grid;
    grid-template-columns: auto auto;
`

const PaddingLeft = styled.div`
    padding-left: 20px;
`

const CircularCard = styled(Card)`
    height: 24px;
    width: 24px;
    border-radius: 50%;
    justify-content: center;
    align-items: center;
`

export function MissionQueueCard({ order, mission, onDeleteMission }: MissionQueueCardProps) {
    const { TranslateText } = useLanguageContext()
    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    const [confirmDeleteDialogOpen, setConfirmDeleteDialogOpen] = useState<boolean>(false)
    const fillColor = tokens.colors.infographic.primary__energy_red_21.hex
    let numberOfTasks = 0
    mission.tasks.forEach((task) => (numberOfTasks += task.inspections.length))
    return (
        <StyledMissionCard key={mission.id} variant="default" style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <HorizontalNonButtonContent onClick={routeChange}>
                    <CircularCard style={{ background: fillColor }}>
                        <Typography variant="body_short_bold">{order}</Typography>
                    </CircularCard>

                    {mission === placeholderMission ? (
                        <PaddingLeft>
                            <DotProgress size={48} color="primary" />
                        </PaddingLeft>
                    ) : (
                        <Button variant="ghost" fullWidth>
                            <Typography variant="body_short_bold">{mission.name}</Typography>
                        </Button>
                    )}
                    <Typography variant="caption" color="#6F6F6F">
                        {TranslateText('Robot')}: {mission.robot.name}
                    </Typography>
                    <Typography variant="caption" color="#6F6F6F">
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
