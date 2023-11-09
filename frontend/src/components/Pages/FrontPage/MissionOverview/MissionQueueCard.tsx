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
    grid-template-columns: 20px 350px auto 100px 180px;
    align-items: center;
    padding: 4px 0px 4px 10px;
    gap: 10px;
`

const StyledDialogContent = styled.div`
    display: flex;
    flex-direction: column;
    gap: 20px;
`

const StyledButtonSection = styled.div`
    display: flex;
    flex-direction: row;
    justify-content: end;
    padding: 8px;
    gap: 8px;
`

const StyledDialog = styled(Dialog)`
    width: 320px;
    padding: 10px;
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
        <StyledMissionCard key={mission.id} style={{ boxShadow: tokens.elevation.raised }}>
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
                    <Typography variant="caption" color={tokens.colors.text.static_icons__tertiary.hex}>
                        {TranslateText('Robot')}: {mission.robot.name}
                    </Typography>
                    <Typography variant="caption" color={tokens.colors.text.static_icons__tertiary.hex}>
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
                <StyledDialog open={confirmDeleteDialogOpen} isDismissable>
                    <Dialog.Header>
                        <Typography variant="h3">{TranslateText('Remove mission')}</Typography>
                    </Dialog.Header>
                    <Dialog.Content>
                        <StyledDialogContent>
                            <Typography variant="body_long">
                                {TranslateText('Please confirm that you want to remove the mission from the queue:')}
                            </Typography>
                            <Typography bold>{mission.name}</Typography>
                        </StyledDialogContent>
                    </Dialog.Content>
                    <StyledButtonSection>
                        <Button
                            onClick={() => {
                                setConfirmDeleteDialogOpen(false)
                            }}
                            variant="outlined"
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
                </StyledDialog>
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
            <Typography variant="caption" color={tokens.colors.text.static_icons__tertiary.hex}>
                {translateEstimatedDuration}: {hours}
                {translateH} {minutes}
                {translateMin}
            </Typography>
        )
    }

    return (
        <Typography variant="caption" color={tokens.colors.text.static_icons__tertiary.hex}>
            {translateNotAvailable}
        </Typography>
    )
}
