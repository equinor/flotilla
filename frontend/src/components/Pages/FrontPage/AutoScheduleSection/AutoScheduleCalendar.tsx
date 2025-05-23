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
    margin: 10px;

    .calendar_default_main {
        height: 100%;
        width: 100%;
    }

    .calendar_default_event_inner {
        color: #fff;
        border: none;
        border-radius: 5px;
        font-size: 10pt;
        opacity: 1;
    }

    .calendar_default_event {
        min-height: 20px;
        overflow: hidden;
        border: 1px solid ${tokens.colors.ui.background__medium.hex};
    }
    .child {
        position: absolute;
        z-index: 1;
        padding: 2px;
        width: 55px;
        height: 25px;
        background-color: ${tokens.colors.ui.background__light.hex};
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

const legendItems = [
    { color: '#85B7A5', label: 'Scheduled' },
    { color: tokens.colors.interactive.primary__resting.hex, label: 'Upcoming' },
    { color: tokens.colors.interactive.disabled__fill.hex, label: 'Skipped' },
    { color: tokens.colors.interactive.disabled__text.hex, label: 'Completed' },
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
        .sort((a, b) => (a.time > b.time ? 1 : -1))

    const dateInDayPilotFormat = (day: DaysOfWeek, time: string) => {
        const d = new Date()
        d.setDate(d.getDate() + ((allDaysStartingSunday.indexOf(day) - d.getDay() + 7) % 7))
        return new DayPilot.Date(new Date(`${d.toISOString().split('T')[0]}T${time}`), true)
    }

    const isSkipDisabled = (day: DaysOfWeek, time: string, mission: MissionDefinition) =>
        selectMissionStatusType(day, time, mission) !== 'ScheduledJob'

    const choseEventColor = (day: DaysOfWeek, time: string, mission: MissionDefinition) => {
        const missionStatusType = selectMissionStatusType(day, time, mission)

        switch (missionStatusType) {
            case MissionStatusType.ScheduledJob:
                return getColorVariant('#85B7A5', mission.name)
            case MissionStatusType.FutureUnstartedJob:
                return getColorVariant(tokens.colors.interactive.primary__resting.hex, mission.name)
            case MissionStatusType.SkippedJob:
                return tokens.colors.interactive.disabled__fill.hex
            case MissionStatusType.PastJob:
                return tokens.colors.interactive.disabled__text.hex
            default:
                return tokens.colors.interactive.primary__resting.hex
        }
    }

    const getColorVariant = (baseColor: string, missionName: string) => {
        // Normalizes the base color to a 6-digit hex format
        let hex = baseColor.replace(/^#/, '')
        if (hex.length === 3) {
            hex = hex
                .split('')
                .map((char) => char + char)
                .join('')
        }

        // Extracts RGB components from the hex color
        let [r, g, b] = [
            parseInt(hex.substring(0, 2), 16),
            parseInt(hex.substring(2, 4), 16),
            parseInt(hex.substring(4, 6), 16),
        ]

        // Generates a hash from the mission name to create a unique offset
        const hash = Array.from(missionName).reduce((acc, char) => acc + char.charCodeAt(0), 0)
        const offset = (hash % 50) - 25 // Offset range: -25 to +24

        // Adjusts RGB values with the offset, ensuring they remain within valid bounds
        ;[r, g, b] = [r, g, b].map((component) => Math.min(255, Math.max(0, component + offset)))

        // Converts the adjusted RGB values back to a hex color
        return `#${[r, g, b].map((component) => component.toString(16).padStart(2, '0')).join('')}`
    }

    const [calendarConfig, setConfig] = useState<CalendarProps>({
        viewType: 'Days',
        height: 600,
        cellHeight: 35,
        durationBarVisible: false,
        headerDateFormat: 'dddd dd/MM',
        startDate: DayPilot.Date.today(),
        days: 7,
        timeFormat: 'Clock24Hours',
        eventDeleteHandling: 'CallBack',
        eventArrangement: 'SideBySide',
        onEventDelete: async (args) => {
            const { text, start } = args.e.data
            const missionId = args.e.data.data.tags.missionId
            const timeOfDay = args.e.data.data.tags.timeOfDay

            const dateString = DayPilot.Date.parse(start, 'yyyy-MM-ddTHH:mm:ss').toString('dddd, dd MMM yyyy HH:mm')
            const confirmed = window.confirm(
                TranslateText('Are you sure you want to delete {0} scheduled for {1}?', [text, dateString])
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
            return {
                id: crypto.randomUUID(),
                text: mission.name,
                start,
                end: start.addMinutes(60),
                backColor: choseEventColor(day, time, mission),
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
                        <span>{label}</span>
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
