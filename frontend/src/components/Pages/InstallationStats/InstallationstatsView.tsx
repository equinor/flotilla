import React, { useEffect, useState } from 'react'
import { BackendAPICaller } from 'api/ApiCaller'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { useRobotContext } from 'components/Contexts/RobotContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { computeMissionStats, GroupedStats } from './InstallationStats'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Table, Typography, ButtonGroup, Button } from '@equinor/eds-core-react'
import styled from 'styled-components'

const StyledButtonGroup = styled(ButtonGroup)`
    margin-bottom: 2rem;
    max-width: 30rem;
`

const StyledTable = styled(Table)`
    width: 70%;
    margin-bottom: 2rem;
`

const StyledTypography = styled(Typography)`
    padding: 16px;
`

const timeSpans = [
    { label: 'Day', value: 'day' },
    { label: 'Week', value: 'week' },
    { label: 'Month', value: 'month' },
    { label: 'Year', value: 'year' },
]

const getEpochRangeFromTimeSpan = (timeSpan: string): { min: number } => {
    const now = new Date()
    const startOfToday = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()))
    let minDate = new Date(startOfToday)

    switch (timeSpan) {
        case 'day':
            break
        case 'week':
            minDate.setUTCDate(minDate.getUTCDate() - 7)
            break
        case 'month':
            minDate.setUTCMonth(minDate.getUTCMonth() - 1)
            break
        case 'year':
            minDate.setUTCFullYear(minDate.getUTCFullYear() - 1)
            break
        default:
            minDate = new Date(0)
    }

    return { min: Math.floor(minDate.getTime() / 1000) }
}

export const MissionStats = () => {
    const [robotStats, setRobotStats] = useState<GroupedStats>({})
    const [loading, setLoading] = useState(true)
    const [timeSpan, setTimeSpan] = useState('day')
    const { installationCode } = useInstallationContext()
    const { enabledRobots } = useRobotContext()
    const { setAlert, setListAlert } = useAlertContext()
    const { TranslateText } = useLanguageContext()

    useEffect(() => {
        const pageSize: number = 100
        const loadStats = async () => {
            setLoading(true)
            try {
                const { min } = getEpochRangeFromTimeSpan(timeSpan)
                const missions = await BackendAPICaller.getMissionRuns({
                    pageSize: pageSize,
                    minCreationTime: min,
                })
                const relevantMissions = missions.content.filter((m) => enabledRobots.some((r) => r.id === m.robot.id))
                const byRobot = computeMissionStats(relevantMissions)
                setRobotStats(byRobot)
            } catch {
                setAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
                setListAlert(
                    AlertType.RequestFail,
                    <FailedRequestAlertListContent translatedMessage={TranslateText('Failed to retrieve missions')} />,
                    AlertCategory.ERROR
                )
            } finally {
                setLoading(false)
            }
        }
        loadStats()
    }, [installationCode, enabledRobots, timeSpan])

    if (loading) return <StyledTypography variant="h4">Loading mission statistics...</StyledTypography>

    const renderStatsTable = (stats: GroupedStats) => (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>Robot</Table.Cell>
                    <Table.Cell>Successful Missions</Table.Cell>
                    <Table.Cell>Failed Missions</Table.Cell>
                    <Table.Cell>Success %</Table.Cell>
                    <Table.Cell>Successful Tasks</Table.Cell>
                    <Table.Cell>Failed Tasks</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {Object.entries(stats).map(([key, stat]) => {
                    const totalMissions = stat.successCount + stat.failureCount
                    const successPercentage = totalMissions > 0 ? (stat.successCount / totalMissions) * 100 + '%' : '—'
                    return (
                        <Table.Row key={key}>
                            <Table.Cell>{key}</Table.Cell>
                            <Table.Cell>{stat.successCount}</Table.Cell>
                            <Table.Cell>{stat.failureCount}</Table.Cell>
                            <Table.Cell>{successPercentage}</Table.Cell>
                            <Table.Cell>{stat.totalTasksSuccess}</Table.Cell>
                            <Table.Cell>{stat.totalTasksFailure}</Table.Cell>
                        </Table.Row>
                    )
                })}
            </Table.Body>
        </StyledTable>
    )

    return (
        <>
            <StyledButtonGroup>
                {timeSpans.map((option) => (
                    <Button
                        key={option.value}
                        variant={timeSpan === option.value ? 'contained' : 'outlined'}
                        onClick={() => setTimeSpan(option.value)}
                    >
                        {option.label}
                    </Button>
                ))}
            </StyledButtonGroup>
            {renderStatsTable(robotStats)}
        </>
    )
}
