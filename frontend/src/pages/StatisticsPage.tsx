import { useContext, useEffect, useState } from 'react'
import { useAssetContext } from 'components/Contexts/AssetContext'
import { AlertType, useAlertContext } from 'components/Contexts/AlertContext'
import { FailedRequestAlertContent, FailedRequestAlertListContent } from 'components/Alerts/FailedRequestAlert'
import { AlertCategory } from 'components/Alerts/AlertsBanner'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { Table, Typography, ButtonGroup, Button, CircularProgress } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useBackendApi } from 'api/UseBackendApi'
import { InstallationContext } from 'components/Contexts/InstallationContext'
import { computeMissionStats, GroupedStats } from './InstallationStats/InstallationStats'
import { Header } from 'components/Header/Header'
import { NavBar } from 'components/Header/NavBar'
import { StyledPage } from 'components/Styles/StyledComponents'

const StyledButtonGroup = styled(ButtonGroup)`
    margin-bottom: 2rem;
    max-width: 30rem;
`

const StyledTable = styled(Table)`
    margin-bottom: 2rem;
`

const StyledTypography = styled(Typography)`
    padding: 16px;
`

const StyledLoading = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 1rem;
    padding: 2rem;
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

export const StatisticsPage = () => {
    const [robotStats, setRobotStats] = useState<GroupedStats>({})
    const [loading, setLoading] = useState(true)
    const [timeSpan, setTimeSpan] = useState('day')
    const { enabledRobots } = useAssetContext()
    const { installation } = useContext(InstallationContext)
    const { setAlert, setListAlert } = useAlertContext()
    const { TranslateText } = useLanguageContext()
    const { alerts } = useAlertContext()
    const backendApi = useBackendApi()

    useEffect(() => {
        const pageSize: number = 100
        const loadStats = async () => {
            setLoading(true)
            try {
                const { min } = getEpochRangeFromTimeSpan(timeSpan)
                const missions = await backendApi.getMissionRuns({
                    installationCode: installation.installationCode,
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
    }, [installation, timeSpan])

    if (loading)
        return (
            <>
                <Header alertDict={alerts} installation={installation} />
                <NavBar />
                <StyledLoading>
                    <CircularProgress />
                    <StyledTypography variant="h4">
                        {TranslateText('Loading mission statistics') + '...'}
                    </StyledTypography>
                </StyledLoading>
            </>
        )

    const StatsTable = ({ stats }: { stats: GroupedStats }) => (
        <StyledTable>
            <Table.Head>
                <Table.Row>
                    <Table.Cell>{TranslateText('Robot')}</Table.Cell>
                    <Table.Cell>{TranslateText('Successful Missions')}</Table.Cell>
                    <Table.Cell>{TranslateText('Partially Successful Missions')}</Table.Cell>
                    <Table.Cell>{TranslateText('Failed Missions')}</Table.Cell>
                    <Table.Cell>{TranslateText('Mission Success Rate')}</Table.Cell>
                    <Table.Cell>{TranslateText('Successful Tasks')}</Table.Cell>
                    <Table.Cell>{TranslateText('Failed Tasks')}</Table.Cell>
                    <Table.Cell>{TranslateText('Task Success Rate')}</Table.Cell>
                </Table.Row>
            </Table.Head>
            <Table.Body>
                {Object.entries(stats).map(([key, stat]) => {
                    const totalMissions = stat.successCount + stat.failureCount
                    const successPercentage =
                        totalMissions > 0 ? ((stat.successCount / totalMissions) * 100).toFixed(1) + '%' : '—'
                    const totalTasks = stat.totalTasksSuccess + stat.totalTasksFailure
                    const taskSuccessPercentage =
                        totalTasks > 0 ? ((stat.totalTasksSuccess / totalTasks) * 100).toFixed(1) + '%' : '—'
                    return (
                        <Table.Row key={key}>
                            <Table.Cell>{key}</Table.Cell>
                            <Table.Cell>{stat.successCount}</Table.Cell>
                            <Table.Cell>{stat.partiallySuccessfulCount}</Table.Cell>
                            <Table.Cell>{stat.failureCount}</Table.Cell>
                            <Table.Cell>{successPercentage}</Table.Cell>
                            <Table.Cell>{stat.totalTasksSuccess}</Table.Cell>
                            <Table.Cell>{stat.totalTasksFailure}</Table.Cell>
                            <Table.Cell>{taskSuccessPercentage}</Table.Cell>
                        </Table.Row>
                    )
                })}
            </Table.Body>
        </StyledTable>
    )

    return (
        <>
            <Header alertDict={alerts} installation={installation} />
            <NavBar />
            <StyledPage>
                <StyledTypography variant="body_short" color="text_secondary">
                    {TranslateText('Statistics info text')}
                </StyledTypography>
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
                <StatsTable stats={robotStats} />
            </StyledPage>
        </>
    )
}
