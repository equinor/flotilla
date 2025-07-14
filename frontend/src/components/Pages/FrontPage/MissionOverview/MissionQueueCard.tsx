import { Button, Card, Dialog, Icon, Typography, DotProgress } from '@equinor/eds-core-react'
import { config } from 'config'
import { Mission } from 'models/Mission'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { tokens } from '@equinor/eds-tokens'
import { calculateRemaindingTimeInMinutes } from 'utils/CalculateRemaingingTime'

interface MissionQueueCardProps {
    order: number
    mission: Mission
    onDeleteMission: (mission: Mission) => void
}

interface RemoveMissionDialogProps {
    confirmDeleteDialogOpen: boolean
    mission: Mission
    setConfirmDeleteDialogOpen: React.Dispatch<React.SetStateAction<boolean>>
    onDeleteMission: (mission: Mission) => void
}

const StyledMissionCard = styled(Card)`
    display: flex;
    flex-direction: row;
    min-height: 48px;
    padding: 4px 16px;
    justify-content: space-between;
    align-items: center;
    align-self: stretch;
    border: 1px solid ${tokens.colors.ui.background__medium.hex};
`
const StyledBody = styled.div`
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: flex-start;
    flex: 1 0 0;
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
const CircularCard = styled(Card)`
    height: 24px;
    width: 24px;
    border-radius: 50%;
    justify-content: center;
    align-items: center;
`
const StyledButton = styled(Button)`
    margin-left: -18px;
    height: auto;
`

export const MissionQueueCard = ({ order, mission, onDeleteMission }: MissionQueueCardProps) => {
    const navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission-${mission.id}`
        navigate(path)
    }
    const [confirmDeleteDialogOpen, setConfirmDeleteDialogOpen] = useState<boolean>(false)
    const fillColor = tokens.colors.infographic.primary__energy_red_21.hex

    return (
        <StyledMissionCard key={mission.id}>
            <CircularCard style={{ background: fillColor }}>
                <Typography variant="body_short_bold">{order}</Typography>
            </CircularCard>
            <StyledBody onClick={routeChange}>
                <StyledButton variant="ghost">
                    <Typography variant="body_short_bold">{mission.name}</Typography>
                </StyledButton>
                <MissionDetails mission={mission} />
            </StyledBody>
            <Button
                variant="ghost_icon"
                onClick={() => {
                    setConfirmDeleteDialogOpen(true)
                }}
            >
                <Icon name={Icons.Remove} size={24} title="more action" />
            </Button>
            <RemoveMissionDialog
                confirmDeleteDialogOpen={confirmDeleteDialogOpen}
                mission={mission}
                setConfirmDeleteDialogOpen={setConfirmDeleteDialogOpen}
                onDeleteMission={onDeleteMission}
            />
        </StyledMissionCard>
    )
}

export const PlaceholderMissionCard = ({ order }: MissionQueueCardProps) => {
    const fillColor = tokens.colors.infographic.primary__energy_red_21.hex
    return (
        <StyledMissionCard style={{ justifyContent: 'start' }}>
            <CircularCard style={{ background: fillColor }}>
                <Typography variant="body_short_bold">{order}</Typography>
            </CircularCard>
            <DotProgress size={48} color="primary" />
        </StyledMissionCard>
    )
}

const RemoveMissionDialog = ({
    confirmDeleteDialogOpen,
    mission,
    setConfirmDeleteDialogOpen,
    onDeleteMission,
}: RemoveMissionDialogProps) => {
    const { TranslateText } = useLanguageContext()

    return (
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
                    {TranslateText('Cancel')}
                </Button>
                <Button
                    onClick={() => {
                        onDeleteMission(mission)
                        setConfirmDeleteDialogOpen(false)
                    }}
                >
                    {TranslateText('Remove mission')}
                </Button>
            </StyledButtonSection>
        </StyledDialog>
    )
}

const MissionDetails = ({ mission }: { mission: Mission }) => {
    const { TranslateText } = useLanguageContext()

    const getEstimatedDuration = () => {
        const translateEstimatedDuration = TranslateText('Estimated duration')
        const translateH = TranslateText('h')
        const translateMin = TranslateText('min')
        const translateNotAvailable = TranslateText('Estimated duration: not available')

        if (mission.estimatedTaskDuration) {
            const estimatedDuration = calculateRemaindingTimeInMinutes(mission.tasks, mission.estimatedTaskDuration)
            const hours = Math.floor(estimatedDuration / 60)
            const remainingMinutes = Math.ceil(estimatedDuration % 60)

            return `${translateEstimatedDuration}: ${hours} ${translateH} ${remainingMinutes} ${translateMin}`
        }
        return translateNotAvailable
    }

    const tasks = `${TranslateText('Tasks')}: ${mission.tasks.length}`
    const missionDetails = `${tasks} | ${getEstimatedDuration()}`

    return (
        <Typography variant="caption" color={tokens.colors.text.static_icons__tertiary.hex}>
            {missionDetails}
        </Typography>
    )
}
