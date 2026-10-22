import { Button, Icon, Popover, Table, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import styled from 'styled-components'
import { Link } from 'react-router'
import { useContext, useState } from 'react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { InstallationContext } from 'contexts/InstallationContext'
import { MissionSchedulingEditDialog } from 'components/Dialogs/MissionEditDialog'
import { phone_width } from 'utils/constants'
import { useNow } from 'hooks/useNow'
import {
    AutoScheduleOccurrence,
    canSkipOccurrence,
    canUnskipOccurrence,
    formatScheduledAt,
} from 'models/AutoScheduleOccurrence'
import { SkipAutoMissionDialog } from './SkipAutoMissionDialog'
import { findOccurrenceForSlot } from 'models/AutoScheduleOccurrence'
import { allDays, DaysOfWeek, getAllDaysIndexOfToday, getCetTimeOnly } from 'models/AutoScheduleFrequency'
import { MissionDefinition } from 'models/MissionDefinition'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'

const StyledTableRow = styled.div`
    display: grid;
    align-items: center;
    gap: 1rem;
    grid-template-columns: 130px auto 16px 130px;
    @media (max-width: ${phone_width}) {
        grid-template-columns: 100px 1fr 16px 100px;
        gap: 0.5rem;
    }
`

const StyledMissionInfo = styled.div`
    display: flex;
    align-items: center;
    gap: 10px;
`

const StyledButton = styled(Button)`
    &&:hover {
        background-color: ${tokens.colors.ui.background__medium.hex};
    }
`

const CalendarButton = styled(Button)`
    max-width: 130px;
`

type RowProps = { occurrence: AutoScheduleOccurrence } | { day: DaysOfWeek; time: string; mission: MissionDefinition }

export const AutoScheduleMissionTableRow = (props: RowProps) => {
    const { TranslateText, language } = useLanguageContext()
    const { autoScheduleOccurrences, autoScheduleError } = useMissionDefinitionsContext()
    const occurrence =
        'occurrence' in props
            ? props.occurrence
            : findOccurrenceForSlot(autoScheduleOccurrences, props.mission.id, props.day, props.time)
    const mission = 'occurrence' in props ? props.occurrence.mission : props.mission
    const { installation } = useContext(InstallationContext)
    const [isSkipDialogOpen, setIsSkipDialogOpen] = useState<boolean>(false)
    const [isEditDialogOpen, setIsEditDialogOpen] = useState<boolean>(false)
    const [referenceElement, setReferenceElement] = useState<HTMLButtonElement | null>(null)
    const [isOpen, setIsOpen] = useState(false)
    const now = useNow()

    const isPast =
        'day' in props && props.day === allDays[getAllDaysIndexOfToday(now)] && props.time <= getCetTimeOnly(now)
    const typographyColor =
        isPast || occurrence?.status === 'Skipped'
            ? tokens.colors.interactive.disabled__text.hex
            : tokens.colors.interactive.primary__resting.hex

    const handleOpen = () => {
        setIsOpen(true)
    }
    const handleClose = () => {
        setIsOpen(false)
    }

    return (
        <Table.Row>
            <Table.Cell>
                <StyledTableRow>
                    <Typography color={typographyColor}>
                        {'occurrence' in props
                            ? formatScheduledAt(props.occurrence.scheduledAt, language)
                            : props.time.slice(0, 5)}
                    </Typography>

                    <StyledMissionInfo>
                        <Typography
                            color={typographyColor}
                            as={Link}
                            to={`/${installation.installationCode}/missiondefinition/${mission.id}`}
                            link
                        >
                            {mission.name}
                        </Typography>
                        {occurrence?.status === 'Skipped' && (
                            <Typography color={typographyColor}>{TranslateText('Skipped')}</Typography>
                        )}

                        {mission.comment && (
                            <>
                                <StyledButton
                                    variant="ghost_icon"
                                    aria-haspopup
                                    aria-expanded={isOpen}
                                    ref={setReferenceElement}
                                    onClick={handleOpen}
                                >
                                    <Icon name={Icons.Info} color={typographyColor} />
                                </StyledButton>

                                <Popover
                                    open={isOpen}
                                    anchorEl={referenceElement}
                                    onClose={handleClose}
                                    placement="top"
                                >
                                    <Popover.Content>
                                        <Typography variant="body_short">{mission.comment}</Typography>
                                    </Popover.Content>
                                </Popover>
                            </>
                        )}
                    </StyledMissionInfo>
                    <CalendarButton variant="ghost_icon" onClick={() => setIsEditDialogOpen(true)}>
                        <Icon name={Icons.Edit} size={16} color={typographyColor} />
                    </CalendarButton>
                    {occurrence && (
                        <CalendarButton
                            variant="ghost"
                            disabled={
                                autoScheduleError ||
                                !(canSkipOccurrence(occurrence, now) || canUnskipOccurrence(occurrence, now))
                            }
                            onClick={() => setIsSkipDialogOpen(true)}
                        >
                            {TranslateText(occurrence.status === 'Skipped' ? 'Undo skip' : 'SkipAutoMission')}
                        </CalendarButton>
                    )}
                    {isSkipDialogOpen && occurrence && (
                        <SkipAutoMissionDialog occurrence={occurrence} onClose={() => setIsSkipDialogOpen(false)} />
                    )}
                    <MissionSchedulingEditDialog
                        mission={mission}
                        isOpen={isEditDialogOpen}
                        onClose={() => setIsEditDialogOpen(false)}
                    />
                </StyledTableRow>
            </Table.Cell>
        </Table.Row>
    )
}
