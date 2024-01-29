import { Button, Card, Dialog, Icon, Typography, DotProgress } from '@equinor/eds-core-react'
import { config } from 'config'
import { Mission, placeholderMission } from 'models/Mission'
import styled from 'styled-components'
import { useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Icons } from 'utils/icons'
import { StyledDialog } from 'components/Styles/StyledComponents'
import { tokens } from '@equinor/eds-tokens'

interface MissionQueueCardProps {
    order: number
    mission: Mission
    onDeleteMission: (mission: Mission) => void
}

interface MissionDisplayProps {
    mission: Mission
}

interface RemoveMissionDialogProps {
    confirmDeleteDialogOpen: boolean
    mission: Mission
    setConfirmDeleteDialogOpen: React.Dispatch<React.SetStateAction<boolean>>
    onDeleteMission: (mission: Mission) => void
}

const StyledMissionCard = styled(Card)`
    display: grid;
    width: calc(100vw - 30px);
    max-width: 880px;
    min-height: 50px;
`
const HorizontalContent = styled.div`
    display: grid;
    grid-template-columns: 40px auto 50px;
    align-items: center;
    padding-left: 10px;
`
const HorizontalNonButtonContent = styled.div`
    @media (min-width: 700px) {
        display: grid;
        grid-template-columns: auto 120px 90px 180px;
        align-items: center;
        padding: 4px 0px 4px 10px;
        gap: 10px;
    }

    @media (max-width: 700px) {
        display: grid;
        grid-template: auto auto / 140px auto;

        #missionName {
            grid-area: 1 / 1 / auto / span 2;
            padding-bottom: 10px;
        }

        #robotName {
            grid-area: 2 / 1 / 140px / span 1;
        }

        #taskProgress {
            grid-area: 2 / 2 / auto / span 1;
            padding-left: 15px;
        }

        #estimatedDuration {
            display: none;
        }

        padding: 4px 0px 10px 4px;
    }
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

const PaddingLeft = styled.div`
    padding-left: 10px;
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

const EllipsisTypography = styled(Typography)`
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`

export const MissionQueueCard = ({ order, mission, onDeleteMission }: MissionQueueCardProps): JSX.Element => {
    const { TranslateText } = useLanguageContext()
    let navigate = useNavigate()
    const routeChange = () => {
        const path = `${config.FRONTEND_BASE_ROUTE}/mission/${mission.id}`
        navigate(path)
    }
    const [confirmDeleteDialogOpen, setConfirmDeleteDialogOpen] = useState<boolean>(false)
    const fillColor = tokens.colors.infographic.primary__energy_red_21.hex
    const numberOfTasks = mission.tasks.length

    return (
        <StyledMissionCard key={mission.id} style={{ boxShadow: tokens.elevation.raised }}>
            <HorizontalContent>
                <CircularCard style={{ background: fillColor }}>
                    <Typography variant="body_short_bold">{order}</Typography>
                </CircularCard>
                {mission === placeholderMission ? (
                    <PaddingLeft>
                        <DotProgress size={48} color="primary" />
                    </PaddingLeft>
                ) : (
                    <>
                        <HorizontalNonButtonContent onClick={routeChange}>
                            <div id="missionName">
                                <StyledButton variant="ghost">
                                    <Typography variant="body_short_bold">{mission.name}</Typography>
                                </StyledButton>
                            </div>
                            <EllipsisTypography
                                id="robotName"
                                variant="caption"
                                color={tokens.colors.text.static_icons__tertiary.hex}
                            >
                                {TranslateText('Robot')}: {mission.robot.name}
                            </EllipsisTypography>
                            <Typography
                                id="taskProgress"
                                variant="caption"
                                color={tokens.colors.text.static_icons__tertiary.hex}
                            >
                                {TranslateText('Tasks')}: {numberOfTasks}
                            </Typography>
                            <div id="estimatedDuration">
                                <MissionDurationDisplay mission={mission} />
                            </div>
                        </HorizontalNonButtonContent>
                        <Button
                            variant="ghost_icon"
                            onClick={() => {
                                setConfirmDeleteDialogOpen(true)
                            }}
                        >
                            <Icon name={Icons.Remove} size={24} title="more action" />
                        </Button>
                    </>
                )}
            </HorizontalContent>
            <RemoveMissionDialog
                confirmDeleteDialogOpen={confirmDeleteDialogOpen}
                mission={mission}
                setConfirmDeleteDialogOpen={setConfirmDeleteDialogOpen}
                onDeleteMission={onDeleteMission}
            />
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

const MissionDurationDisplay = ({ mission }: MissionDisplayProps) => {
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
