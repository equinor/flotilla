import { DayPilot, DayPilotCalendar } from 'daypilot-pro-react'
import { useEffect, useMemo } from 'react'
import { useMissionDefinitionsContext } from 'components/Contexts/MissionDefinitionsContext'
import { allDaysStartingSunday, DaysOfWeek } from 'models/AutoScheduleFrequency'
import styled from 'styled-components'
import { useState } from 'react'
import { MissionDefinition } from 'models/MissionDefinition'
import { MissionStatusType, selectMissionStatusType, skipAutoScheduledMission } from './AutoScheduleMissionTableRow'
import { tokens } from '@equinor/eds-tokens'
import type { CalendarProps } from 'daypilot-pro-react'
import { useNavigate } from 'react-router-dom'
import { config } from 'config'
import { useLanguageContext } from 'components/Contexts/LanguageContext'

const CalendarProWrapper = styled.div`
    height: 80vh;
    width: 90vw;
    margin: 2px;

    .calendar_default_main {
        height: 100%;
        width: 100%;
        border: none;
        border-bottom: 1px solid ${tokens.colors.ui.background__medium.hex};
        font-family: Equinor;
    }

    .calendar_default_event_inner {
        font-size: 10pt;
        border-radius: 4px;
        border: 1px solid;
    }

    .calendar_default_event {
        overflow: hidden;
    }
    .child {
        position: absolute;
        z-index: 1;
        padding: 2px;
        width: 55px;
        height: 25px;
        background-color: ${tokens.colors.ui.background__default.hex};
    }

    .calendar_default_rowheader_inner {
        font-size: 20px;
        border-bottom: none;
        background-color: ${tokens.colors.ui.background__default.hex};
    }

    .calendar_default_colheader_inner {
        font-size: 15px;
        border-right: none;
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

const CalendarColors = {
    ScheduledBackground: tokens.colors.infographic.primary__lichen_green.hex,
    ScheduledBorder: tokens.colors.interactive.success__resting.hex,
    UpcomingBackground: tokens.colors.infographic.primary__moss_green_13.hex,
    UpcomingBorder: tokens.colors.infographic.primary__moss_green_100.hex,
    SkippedBackground: tokens.colors.ui.background__default.hex,
    SkippedBorder: tokens.colors.interactive.disabled__text.hex,
    CompletedBackground: '#f7f7f7',
    CompletedBorder: tokens.colors.interactive.disabled__text.hex,
}

const legendItems = [
    { color: CalendarColors.ScheduledBackground, label: 'Scheduled' },
    { color: CalendarColors.UpcomingBackground, label: 'Upcoming' },
    { color: CalendarColors.SkippedBackground, label: 'Skipped' },
    { color: CalendarColors.CompletedBackground, label: 'Completed' },
]

export const CalendarPro = () => {
    const { missionDefinitions } = useMissionDefinitionsContext()
    const navigate = useNavigate()
    const { TranslateText } = useLanguageContext()

    const autoScheduledMissionDefinitions = missionDefinitions.filter((m) => m.autoScheduleFrequency)
    const timeMissionPairs = autoScheduledMissionDefinitions
        .flatMap((mission) =>
            mission.autoScheduleFrequency!.daysOfWeek.flatMap((day) =>
                mission.autoScheduleFrequency!.timesOfDayCET.map((time) => {
                    return { day, time, mission }
                })
            )
        )
        .sort((a, b) => (a.time === b.time ? 0 : a.time > b.time ? 1 : -1))

    const dateInDayPilotFormat = (day: DaysOfWeek, time: string) => {
        const d = new Date()
        d.setDate(d.getDate() + ((allDaysStartingSunday.indexOf(day) - d.getDay() + 7) % 7))
        return new DayPilot.Date(new Date(`${d.toISOString().split('T')[0]}T${time}`), true)
    }

    const isSkipDisabled = (day: DaysOfWeek, time: string, mission: MissionDefinition) =>
        selectMissionStatusType(day, time, mission) !== MissionStatusType.ScheduledJob

    const choseEventColors = (day: DaysOfWeek, time: string, mission: MissionDefinition) => {
        const missionStatusType = selectMissionStatusType(day, time, mission)

        switch (missionStatusType) {
            case MissionStatusType.ScheduledJob:
                return {
                    backColor: CalendarColors.ScheduledBackground,
                    borderColor: CalendarColors.ScheduledBorder,
                }
            case MissionStatusType.FutureUnstartedJob:
                return {
                    backColor: CalendarColors.UpcomingBackground,
                    borderColor: CalendarColors.UpcomingBorder,
                }
            case MissionStatusType.SkippedJob:
                return {
                    backColor: CalendarColors.SkippedBackground,
                    borderColor: CalendarColors.SkippedBorder,
                }
            default:
                return {
                    backColor: CalendarColors.CompletedBackground,
                    borderColor: CalendarColors.CompletedBorder,
                }
        }
    }

    const [calendarConfig, setConfig] = useState<CalendarProps>({
        viewType: 'Days',
        height: 600,
        cellHeight: 35,
        durationBarVisible: false,
        headerDateFormat: 'dd dddd',
        startDate: DayPilot.Date.today(),
        days: 7,
        timeFormat: 'Clock24Hours',
        cellDuration: 30,
        eventDeleteHandling: 'CallBack',
        eventArrangement: 'SideBySide',
        eventMoveHandling: 'Disabled',
        eventResizeHandling: 'Disabled',
        onEventDelete: async (args) => {
            const { text, start } = args.e.data
            const missionId = args.e.data.data.tags.missionId
            const timeOfDay = args.e.data.data.tags.timeOfDay

            const dateString = DayPilot.Date.parse(start, 'yyyy-MM-ddTHH:mm:ss').toString('HH:mm')
            const confirmed = window.confirm(
                TranslateText('Are you sure you want to remove {0} scheduled for today at {1}?', [text, dateString])
            )
            if (!confirmed) return

            await skipAutoScheduledMission(missionId, timeOfDay)
            setConfig((prev) => ({
                ...prev,
                events: prev.events?.filter((e) => e.id !== args.e.id()) || [],
            }))
        },
        onEventClicked: async (args) => {
            const missionId = args.e.data.data.tags.missionId
            navigate(`${config.FRONTEND_BASE_ROUTE}/mission-definition/${missionId}`)
        },
        events: [],
    })

    const events = useMemo(() => {
        return timeMissionPairs.map(({ day, time, mission }) => {
            const start = dateInDayPilotFormat(day, time)
            const { backColor, borderColor } = choseEventColors(day, time, mission)

            const isSkipped = selectMissionStatusType(day, time, mission) === MissionStatusType.SkippedJob
            const displayText = isSkipped ? `${mission.name}` + ' (' + TranslateText('Skipped') + ')' : mission.name

            return {
                id: crypto.randomUUID(),
                text: displayText,
                start: start,
                end: start.addMinutes(31),
                backColor: backColor,
                borderColor: borderColor,
                deleteDisabled: isSkipDisabled(day, time, mission),
                data: {
                    tags: {
                        missionId: mission.id,
                        timeOfDay: time,
                    },
                },
            }
        })
    }, [missionDefinitions])

    useEffect(() => {
        setConfig((prev) => ({
            ...prev,
            events,
        }))
    }, [missionDefinitions])

    return (
        <>
            <LegendWrapper>
                {legendItems.map(({ color, label }) => (
                    <LegendItem key={label}>
                        <LegendColor color={color} />
                        <span>{TranslateText(label)}</span>
                    </LegendItem>
                ))}
            </LegendWrapper>
            <CalendarProWrapper>
                <div className="child"></div>
                <DayPilotCalendar {...calendarConfig} />
            </CalendarProWrapper>
        </>
    )
}
