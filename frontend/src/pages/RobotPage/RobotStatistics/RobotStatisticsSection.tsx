import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { Card, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { FieldLabel, subtleCardShadow } from 'components/Styles/StyledComponents'
import { phone_width } from 'utils/constants'
import { useRobotStatistics } from 'hooks/useRobotStatistics'
import { DonutChart } from './DonutChart'
import { WeeklyBarChart } from './WeeklyBarChart'

const Section = styled.div`
    display: flex;
    flex-direction: column;
    gap: 1rem;
`
const SectionHeading = styled.div`
    display: flex;
    align-items: baseline;
    gap: 8px;
`
const HeadingTitle = styled(Typography)`
    font-family: Equinor, sans-serif;
    font-size: 0.82rem;
    font-weight: 700;
    letter-spacing: 0.12em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__default.hex};
`
const HeadingSubtitle = styled(Typography)`
    font-family: Equinor, sans-serif;
    font-size: 0.82rem;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`
const CardsRow = styled.div`
    display: flex;
    flex-wrap: wrap;
    gap: 24px;
`
const StatCard = styled(Card)`
    display: flex;
    flex-direction: column;
    gap: 16px;
    flex: 1 1 320px;
    min-width: 280px;
    padding: 20px 24px;
    box-sizing: border-box;
    box-shadow: ${subtleCardShadow};
    @media (max-width: ${phone_width}) {
        padding: 16px;
    }
`
const CardHeaderRow = styled.div`
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    gap: 8px;
`
const MutedCaption = styled(Typography)`
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`
const DonutRow = styled.div`
    display: flex;
    align-items: center;
    gap: 24px;
`
const DonutInfo = styled.div`
    display: flex;
    flex-direction: column;
    gap: 8px;
`
const BigNumber = styled(Typography)`
    font-family: Equinor, sans-serif;
    font-size: 2rem;
    font-weight: 700;
    line-height: 1;
    color: ${tokens.colors.text.static_icons__default.hex};
`
const Denominator = styled(Typography)`
    font-size: 1.25rem;
    font-weight: 400;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`
const Legend = styled.div`
    display: flex;
    flex-direction: column;
    gap: 6px;
`
const LegendRow = styled.div`
    display: flex;
    align-items: center;
    gap: 8px;
`
const LegendSwatch = styled.span<{ $color: string }>`
    width: 12px;
    height: 12px;
    border-radius: 2px;
    flex-shrink: 0;
    background: ${({ $color }) => $color};
`
const StateMessage = styled.div`
    display: flex;
    padding: 2rem 0;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`

interface DonutStatCardProps {
    label: string
    fraction: number
    color: string
    caption: string
    successCount: number
    totalCount: number
    ratioCaption: string
    successLegend: string
    restCount: number
    restLegend: string
}

const DonutStatCard = ({
    label,
    fraction,
    color,
    caption,
    successCount,
    totalCount,
    ratioCaption,
    successLegend,
    restCount,
    restLegend,
}: DonutStatCardProps) => (
    <StatCard>
        <FieldLabel>{label}</FieldLabel>
        <DonutRow>
            <DonutChart fraction={fraction} color={color} caption={caption} />
            <DonutInfo>
                <BigNumber>
                    {successCount}
                    <Denominator as="span"> / {totalCount}</Denominator>
                </BigNumber>
                <MutedCaption variant="caption">{ratioCaption}</MutedCaption>
                <Legend>
                    <LegendRow>
                        <LegendSwatch $color={color} />
                        <Typography variant="caption">{`${successCount} ${successLegend}`}</Typography>
                    </LegendRow>
                    <LegendRow>
                        <LegendSwatch $color={tokens.colors.ui.background__medium.hex} />
                        <Typography variant="caption">{`${restCount} ${restLegend}`}</Typography>
                    </LegendRow>
                </Legend>
            </DonutInfo>
        </DonutRow>
    </StatCard>
)

export const RobotStatisticsSection = ({ robotId }: { robotId: string }) => {
    const { TranslateText } = useLanguageContext()
    const { data: statistics, isPending, isError } = useRobotStatistics(robotId)

    const heading = (
        <SectionHeading>
            <HeadingTitle>{TranslateText('Performance')}</HeadingTitle>
            <HeadingSubtitle>{`· ${TranslateText('Last 30 days')}`}</HeadingSubtitle>
        </SectionHeading>
    )

    if (isPending || isError || statistics.missions.total === 0) {
        const message = isPending
            ? TranslateText('Loading statistics') + '...'
            : isError
              ? TranslateText('Could not load statistics')
              : TranslateText('No missions in the last 30 days')
        return (
            <Section>
                {heading}
                <StateMessage>
                    <Typography variant="body_short">{message}</Typography>
                </StateMessage>
            </Section>
        )
    }

    const { missions, tasks, missionsPerWeek } = statistics
    const missionsSuccessful = missions.successful + missions.partiallySuccessful
    const missionsUnsuccessful = Math.max(missions.total - missionsSuccessful, 0)
    const tasksSuccessful = tasks.successful + tasks.partiallySuccessful
    const tasksIncomplete = Math.max(tasks.total - tasksSuccessful, 0)
    const weeklyData = missionsPerWeek.map((week, index) => ({
        label: `${TranslateText('Wk')} ${index + 1}`,
        value: week.count,
    }))
    const weeklyAverage = weeklyData.length
        ? weeklyData.reduce((sum, week) => sum + week.value, 0) / weeklyData.length
        : 0

    return (
        <Section>
            {heading}
            <CardsRow>
                <DonutStatCard
                    label={TranslateText('Mission success')}
                    fraction={missions.successRate}
                    color={tokens.colors.interactive.primary__resting.hex}
                    caption={TranslateText('success rate')}
                    successCount={missionsSuccessful}
                    totalCount={missions.total}
                    ratioCaption={TranslateText('missions successful / run')}
                    successLegend={TranslateText('successful')}
                    restCount={missionsUnsuccessful}
                    restLegend={TranslateText('failed / aborted')}
                />
                <DonutStatCard
                    label={TranslateText('Task completion')}
                    fraction={tasks.successRate}
                    color={tokens.colors.interactive.primary__resting.hex}
                    caption={TranslateText('completed')}
                    successCount={tasksSuccessful}
                    totalCount={tasks.total}
                    ratioCaption={TranslateText('tasks successful / total')}
                    successLegend={TranslateText('successful')}
                    restCount={tasksIncomplete}
                    restLegend={TranslateText('incomplete')}
                />
                <StatCard>
                    <CardHeaderRow>
                        <FieldLabel>{TranslateText('Missions per week')}</FieldLabel>
                        <MutedCaption variant="caption">
                            {`${TranslateText('avg')} ${weeklyAverage.toFixed(1)} / ${TranslateText('wk')}`}
                        </MutedCaption>
                    </CardHeaderRow>
                    <WeeklyBarChart data={weeklyData} />
                </StatCard>
            </CardsRow>
        </Section>
    )
}
