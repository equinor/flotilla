import { Calendar, dateFnsLocalizer, Views } from 'react-big-calendar'
import { format, parse, startOfWeek, getDay } from 'date-fns'
import { nb } from 'date-fns/locale'
import 'react-big-calendar/lib/css/react-big-calendar.css'
import styled from 'styled-components'
import { useMemo, useState } from 'react'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { allDaysStartingSunday, DaysOfWeek } from 'models/AutoScheduleFrequency'
import { MissionStatusType, selectMissionStatusType, skipAutoScheduledMission } from './AutoScheduleMissionTableRow'
import { tokens } from '@equinor/eds-tokens'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Button, Typography } from '@equinor/eds-core-react'
import { StyledDialog } from 'components/Styles/StyledComponents'

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
        background: '#f7f7f7',
        border: tokens.colors.interactive.disabled__text.hex,
    },
}

const CalendarWrapper = styled.div`
    height: 70vh;
    width: 90vw;
    margin: 2px;

    .rbc-calendar {
        border: none;
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

    .rbc-day-bg {
        display: none;
    }

    .rbc-day-slot .rbc-event {
        display: block;
    }

    .rbc-event {
        padding: 2px !important;
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

    .rbc-time-gutter {
        text-align: center;
        background-color: ${tokens.colors.ui.background__default.hex};
    }
`

const LegendWrapper = styled.div`
    display: flex;
    gap: 16px;
    margin-bottom: 12px;
    font-size: 14px;
    align-items: center;
    flex-wrap: wrap;
    align-self: flex-start;
    font-family: Equinor;
`

const LegendItem = styled.div`
    display: flex;
    align-items: center;
    gap: 6px;
`

const LegendColor = styled.span<{ color: string }>`
    width: 20px;
    height: 16px;
    background-color: ${({ color }) => color};
    border: 1px solid #ccc;
    border-radius: 0;
`

const StyledEvent = styled.div`
    position: relative,
    width: 100%;
    height: 100%;
    padding: 4px;
    box-sizing: border-box;
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
    background-color: ${CalendarColors.Planned.background};
    color: ${tokens.colors.text.static_icons__default.hex};
    border: 1px solid ${CalendarColors.Planned.border};

    &:hover {
        background-color: ${tokens.colors.ui.background__default.hex};
        border: 1px solid ${CalendarColors.Planned.border};
    }
`

const StyledDialogActions = styled(StyledDialog.Actions)`
    display: flex;
    flex-direction: row;
    gap: 6px;
`

const legendItems = [
    { color: CalendarColors.Planned, label: 'Planned today' },
    { color: CalendarColors.Future, label: 'Future missions' },
    { color: CalendarColors.Skipped, label: 'Skipped' },
    { color: CalendarColors.Passed, label: 'Passed' },
]

export const CalendarPro = () => {
    const { missionDefinitions } = useMissionDefinitionsContext()
    const { TranslateText } = useLanguageContext()
    const navigate = useNavigate()
    const [dialogOpen, setDialogOpen] = useState<boolean>(false)
    const [selectedEvent, setSelectedEvent] = useState<any>(null)

    const getTargetDate = (day: DaysOfWeek, time: string) => {
        const today = new Date()
        const offset = (allDaysStartingSunday.indexOf(day) - today.getDay() + 7) % 7
        const targetDate = new Date(today)
        targetDate.setDate(today.getDate() + offset)
        const [hours, minutes] = time.split(':').map(Number)
        targetDate.setHours(hours, minutes, 0, 0)
        return targetDate
    }

    const events = useMemo(() => {
        return missionDefinitions
            .filter((m) => m.autoScheduleFrequency)
            .flatMap((mission) => {
                return mission.autoScheduleFrequency!.daysOfWeek.flatMap((day) => {
                    return mission.autoScheduleFrequency!.timesOfDayCET.map((time) => {
                        const targetDate = getTargetDate(day, time)
                        const status = selectMissionStatusType(day, time, mission)
                        const color =
                            status === MissionStatusType.ScheduledJob
                                ? CalendarColors.Planned
                                : status === MissionStatusType.FutureUnstartedJob
                                  ? CalendarColors.Future
                                  : status === MissionStatusType.SkippedJob
                                    ? CalendarColors.Skipped
                                    : CalendarColors.Passed

                        return {
                            id: `${mission.id}-${day}-${time}`,
                            title: `${mission.name}${status === MissionStatusType.SkippedJob ? ' (' + TranslateText('Skipped') + ')' : ''}`,
                            start: targetDate,
                            end: new Date(targetDate.getTime() + 59 * 60 * 1000),
                            skip: status === MissionStatusType.ScheduledJob,
                            resource: mission.id,
                            color,
                            status,
                            metadata: {
                                missionName: mission.name,
                                missionId: mission.id,
                                time,
                            },
                        }
                    })
                })
            })
    }, [missionDefinitions])

    const handleCloseDialog = () => {
        setDialogOpen(false)
    }

    const handleSelectEvent = (event: any) => {
        setSelectedEvent(event)
        setDialogOpen(true)
    }

    const renderDialog = () => (
        <StyledDialog open={dialogOpen} onClose={handleCloseDialog}>
            <StyledDialog.Header>
                <Typography variant="h3">
                    {TranslateText('SkipAutoMission') + ' ' + TranslateText('Mission')}
                </Typography>
            </StyledDialog.Header>
            <StyledDialog.Content>
                {TranslateText('Are you sure you want to skip {0} scheduled for today at {1}?', [
                    selectedEvent.metadata.missionName,
                    selectedEvent.metadata.time.slice(0, 5),
                ])}
            </StyledDialog.Content>
            <StyledDialogActions>
                <Button onClick={handleCloseDialog} variant="outlined" color="primary">
                    {TranslateText('Cancel')}
                </Button>
                <Button
                    onClick={async () => {
                        if (selectedEvent) {
                            await skipAutoScheduledMission(
                                selectedEvent.metadata.missionId,
                                selectedEvent.metadata.time
                            )
                        }
                        setDialogOpen(false)
                        setSelectedEvent(null)
                    }}
                    variant="outlined"
                    color="danger"
                >
                    {TranslateText('SkipAutoMission')}
                </Button>
            </StyledDialogActions>
        </StyledDialog>
    )

    const CustomEvent = ({ event }: { event: any }) => {
        return (
            <StyledEvent>
                <span>{event.title}</span>
                {event.skip && (
                    <StyledSkipButton onClick={() => handleSelectEvent(event)}>
                        {TranslateText('SkipAutoMission')}
                    </StyledSkipButton>
                )}
            </StyledEvent>
        )
    }

    return (
        <>
            <LegendWrapper>
                {legendItems.map(({ color, label }) => (
                    <LegendItem key={label}>
                        <LegendColor color={color.background} />
                        <span>{TranslateText(label)}</span>
                    </LegendItem>
                ))}
            </LegendWrapper>
            <CalendarWrapper>
                <Calendar
                    localizer={localizer}
                    events={events}
                    components={{ event: CustomEvent }}
                    defaultView={Views.WEEK}
                    views={[Views.WEEK]}
                    startAccessor="start"
                    endAccessor="end"
                    step={60}
                    timeslots={1}
                    formats={{
                        timeGutterFormat: 'HH:mm', // 24-hour format for gutter
                        eventTimeRangeFormat: () => '',
                    }}
                    eventPropGetter={(event) => ({
                        style: {
                            backgroundColor: event.color.background,
                            borderRadius: '4px',
                            border: `1px solid ${event.color.border}`,
                            color: '#000',
                        },
                    })}
                    onDoubleClickEvent={(event) =>
                        navigate(`${config.FRONTEND_BASE_ROUTE}/missiondefinition-${event.metadata.missionId}`)
                    }
                />
            </CalendarWrapper>
            {dialogOpen && renderDialog()}
        </>
    )
}
