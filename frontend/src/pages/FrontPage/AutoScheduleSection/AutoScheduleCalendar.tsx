import { Calendar, dateFnsLocalizer, Views } from 'react-big-calendar'
import { format, parse, startOfWeek, getDay } from 'date-fns'
import { nb } from 'date-fns/locale'
import 'react-big-calendar/lib/css/react-big-calendar.css'
import styled from 'styled-components'
import { useContext, useMemo, useState } from 'react'
import { allDaysStartingSunday, DaysOfWeek, getAutoScheduledOccurrences, getCetNow } from 'models/AutoScheduleFrequency'
import {
    AutoScheduleOccurrence,
    canSkipOccurrence,
    canUnskipOccurrence,
    findOccurrenceForSlot,
} from 'models/AutoScheduleOccurrence'
import { SkipAutoMissionDialog } from './SkipAutoMissionDialog'
import { useNavigate } from 'react-router'
import { Button, Chip, Icon, Typography } from '@equinor/eds-core-react'
import { subtleCardShadow } from 'components/Styles/StyledComponents'
import { useMissionDefinitionsContext } from 'contexts/MissionDefinitionsContext'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'contexts/LanguageContext'
import { InstallationContext } from 'contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { MissionSchedulingEditDialog } from 'components/Dialogs/MissionEditDialog'
import { MissionDefinition } from 'models/MissionDefinition'
import { useNow } from 'hooks/useNow'

const locales = { nb }
const localizer = dateFnsLocalizer({ format, parse, startOfWeek, getDay, locales })

const CalendarColors = {
    Planned: {
        background: tokens.colors.infographic.primary__lichen_green.hex,
        border: tokens.colors.interactive.success__resting.hex,
    },
    Future: {
        background: tokens.colors.infographic.primary__moss_green_13.hex,
        border: tokens.colors.infographic.primary__moss_green_100.hex,
    },
    Skipped: {
        background: tokens.colors.ui.background__default.hex,
        border: tokens.colors.interactive.disabled__text.hex,
    },
    Passed: {
        background: tokens.colors.ui.background__light.hex,
        border: tokens.colors.interactive.disabled__text.hex,
    },
}

const CalendarWrapper = styled.div`
    height: 70vh;
    width: 100%;
    margin: 2px;

    .rbc-calendar {
        border: none;
    }
    .rbc-toolbar {
        justify-content: start;
    }
    .rbc-toolbar-label {
        display: none;
    }

    .rbc-time-view {
        border-left: none;
        border-top: none;
    }

    .rbc-row {
        min-height: 0px;
    }

    .rbc-header {
        min-height: 30px;
        align-content: center;
        background-color: ${tokens.colors.ui.background__default.hex};
    }

    .rbc-header.rbc-today {
        color: ${tokens.colors.interactive.primary__resting.hex};
        font-weight: 600;
        border-bottom: 2px solid ${tokens.colors.interactive.primary__resting.hex};
    }

    .rbc-day-bg {
        display: none;
    }

    .rbc-day-slot .rbc-event {
        display: block;
    }

    .rbc-event {
        padding: 2px !important;
        cursor: pointer;
        transition: box-shadow 0.15s ease-in-out;
    }

    .rbc-event:hover {
        box-shadow: 0 4px 10px rgba(0, 0, 0, 0.18) !important;
    }

    .rbc-event-content {
        font-size: 10pt;
        font-family: Equinor;
        word-break: break-word;
        white-space: normal;
    }

    .rbc-time-header-content,
    .rbc-time-gutter,
    .rbc-time-content {
        font-family: Equinor;
    }

    .rbc-time-slot {
        align-content: top;
        padding-top: 10px;
        min-height: 80px;
    }
    .rbc-time-content {
        border-top: none;
    }

    .rbc-timeslot-group {
        border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
    }

    .rbc-time-content > * + * > * {
        border-left: none;
    }

    .rbc-day-slot .rbc-time-slot {
        border-top: none;
    }

    .rbc-time-gutter {
        text-align: center;
        background-color: ${tokens.colors.ui.background__default.hex};
    }

    .rbc-current-time-indicator {
        height: 1.5px;
        background-color: ${tokens.colors.interactive.primary__resting.hex};
    }
`

const LegendWrapper = styled.div`
    display: flex;
    gap: 8px;
    margin-bottom: 12px;
    align-items: center;
    flex-wrap: wrap;
    align-self: flex-start;
`
const StyledEvent = styled.div`
    position: relative,
    width: 100%;
    height: 100%;
    padding: 4px;
    display: flex;
    flex-direction: column;
    justify-content: flex-start;

    span {
    overflow: hidden;
    white-space: wrap;
    text-overflow: ellipsis;
  }
`

const StyledSkipButton = styled(Button)`
    position: absolute;
    bottom: 4px;
    left: 4px;
    padding: 2px 6px;
    cursor: pointer;
    font-size: 12px;
    height: 25px;
    border-radius: 4px;
    background-color: ${CalendarColors.Planned.background};
    color: ${tokens.colors.text.static_icons__default.hex};
    border: 1px solid ${CalendarColors.Planned.border};

    &:hover {
        background-color: ${tokens.colors.ui.background__default.hex};
        border: 1px solid ${CalendarColors.Planned.border};
    }
`

const StyledEditButton = styled(Button)`
    position: absolute;
    bottom: 4px;
    right: 4px;
    width: 25px;
    height: 25px;

    &:hover {
        background-color: ${tokens.colors.ui.background__default.hex};
    }
`

const legendItems = [
    { color: CalendarColors.Planned, label: 'Planned in the next 24 hours' },
    { color: CalendarColors.Future, label: 'Future missions' },
    { color: CalendarColors.Skipped, label: 'Skipped' },
    { color: CalendarColors.Passed, label: 'Passed' },
]

interface ScheduleEvent {
    id: string
    title: string
    start: Date
    end: Date
    occurrence?: AutoScheduleOccurrence
    color: (typeof CalendarColors)[keyof typeof CalendarColors]
    mission: MissionDefinition
}

export const CalendarPro = () => {
    const { missionDefinitions, autoScheduleOccurrences, autoScheduleError } = useMissionDefinitionsContext()
    const { TranslateText } = useLanguageContext()
    const { installation } = useContext(InstallationContext)
    const navigate = useNavigate()
    const [selectedMissionToEdit, setSelectedMissionToEdit] = useState<MissionDefinition>()
    const [selectedOccurrence, setSelectedOccurrence] = useState<AutoScheduleOccurrence>()
    const now = useNow()

    const getTargetDate = (day: DaysOfWeek, time: string, now: Date) => {
        const cetNow = getCetNow(now)
        const offset = (allDaysStartingSunday.indexOf(day) - cetNow.getDay() + 7) % 7
        const targetDate = new Date(cetNow)
        targetDate.setDate(cetNow.getDate() + offset)
        const hours = time.split(':').map(Number)[0]
        const minutes = time.split(':').map(Number)[1]
        targetDate.setHours(hours, minutes, 0, 0)
        return targetDate
    }

    const events = useMemo<ScheduleEvent[]>(() => {
        return getAutoScheduledOccurrences(missionDefinitions).map(({ mission, dayOfWeek, timeOfDay }) => {
            const targetDate = getTargetDate(dayOfWeek, timeOfDay, now)
            const occurrence = findOccurrenceForSlot(autoScheduleOccurrences, mission.id, dayOfWeek, timeOfDay)
            const color =
                occurrence?.status === 'Skipped'
                    ? CalendarColors.Skipped
                    : occurrence?.status === 'Scheduled'
                      ? CalendarColors.Planned
                      : targetDate <= getCetNow(now)
                        ? CalendarColors.Passed
                        : CalendarColors.Future

            return {
                id: `${mission.id}-${dayOfWeek}-${timeOfDay}`,
                title: `${mission.name}${occurrence?.status === 'Skipped' ? ' (' + TranslateText('Skipped') + ')' : ''}`,
                start: targetDate,
                end: new Date(
                    Math.min(
                        targetDate.getTime() + 59 * 60 * 1000,
                        new Date(targetDate.getFullYear(), targetDate.getMonth(), targetDate.getDate() + 1).getTime()
                    )
                ),
                occurrence,
                color,
                mission,
            }
        })
    }, [missionDefinitions, autoScheduleOccurrences, now, TranslateText])

    const CustomEvent = ({ event }: { event: ScheduleEvent }) => {
        return (
            <StyledEvent>
                <Typography variant="caption" as="span" color={tokens.colors.text.static_icons__default.hex}>
                    {event.title}
                </Typography>
                <StyledEditButton variant="ghost_icon" onClick={() => setSelectedMissionToEdit(event.mission)}>
                    <Icon name={Icons.Edit} size={16} color={event.color.border} />
                </StyledEditButton>
                {!autoScheduleError &&
                    event.occurrence &&
                    (canSkipOccurrence(event.occurrence, now) || canUnskipOccurrence(event.occurrence, now)) && (
                        <StyledSkipButton onClick={() => setSelectedOccurrence(event.occurrence)}>
                            {TranslateText(event.occurrence.status === 'Skipped' ? 'Undo skip' : 'SkipAutoMission')}
                        </StyledSkipButton>
                    )}
            </StyledEvent>
        )
    }

    const weekdayShort = (d: Date) => {
        const map: Record<number, string> = {
            0: TranslateText('Sun'),
            1: TranslateText('Mon'),
            2: TranslateText('Tue'),
            3: TranslateText('Wed'),
            4: TranslateText('Thu'),
            5: TranslateText('Fri'),
            6: TranslateText('Sat'),
        }
        return map[d.getDay()]
    }

    return (
        <>
            {autoScheduleError && <Typography>{TranslateText('Failed to retrieve scheduled missions')}</Typography>}
            <LegendWrapper>
                {legendItems.map(({ color, label }) => (
                    <Chip key={label} style={{ background: color.background, borderColor: color.border }}>
                        {TranslateText(label)}
                    </Chip>
                ))}
            </LegendWrapper>
            <CalendarWrapper>
                <Calendar
                    localizer={localizer}
                    events={events}
                    components={{ event: CustomEvent }}
                    defaultView={Views.WEEK}
                    defaultDate={getCetNow(now)}
                    getNow={() => getCetNow(now)}
                    views={[Views.WEEK]}
                    startAccessor="start"
                    endAccessor="end"
                    step={60}
                    timeslots={1}
                    formats={{
                        timeGutterFormat: 'HH:mm', // 24-hour format for gutter
                        eventTimeRangeFormat: () => '',
                        dayFormat: (date) => `${format(date, 'd')} ${weekdayShort(date)}`,
                    }}
                    messages={{
                        today: TranslateText('Today'),
                        previous: TranslateText('Before'),
                        next: TranslateText('Next'),
                    }}
                    eventPropGetter={(event) => ({
                        style: {
                            backgroundColor: event.color.background,
                            borderRadius: '4px',
                            border: `1px solid ${tokens.colors.ui.background__medium.hex}`,
                            borderLeft: `4px solid ${event.color.border}`,
                            boxShadow: subtleCardShadow,
                            color: tokens.colors.text.static_icons__default.hex,
                        },
                    })}
                    onDoubleClickEvent={(event) =>
                        navigate(`/${installation.installationCode}/missiondefinition/${event.mission.id}`)
                    }
                />
            </CalendarWrapper>
            {selectedOccurrence && (
                <SkipAutoMissionDialog
                    occurrence={selectedOccurrence}
                    onClose={() => setSelectedOccurrence(undefined)}
                />
            )}
            {selectedMissionToEdit && (
                <MissionSchedulingEditDialog
                    mission={selectedMissionToEdit}
                    isOpen={!!selectedMissionToEdit}
                    onClose={() => setSelectedMissionToEdit(undefined)}
                />
            )}
        </>
    )
}
