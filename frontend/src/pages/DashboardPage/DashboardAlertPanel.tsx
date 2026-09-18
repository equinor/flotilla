import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ContentCard } from 'components/Styles/StyledComponents'
import { AnalysisEvaluation } from 'models/analysis/AnalysisEvaluation'
import { AnalysisSeverity } from 'models/analysis/AnalysisSeverity'
import { SaraAlertCard } from './AlertComponent'
import { SeverityCountChip } from './SeverityIndicator'

const AlertPanelCard = styled(ContentCard)`
    flex: 1;
    min-height: 0;
    overflow-y: auto;
`

const AlertList = styled.div`
    display: flex;
    flex-direction: column;
    flex: 1;
    min-height: 100%;
    gap: 16px;
`

const PanelHeader = styled.div`
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: ${tokens.spacings.comfortable.medium};
    padding-bottom: ${tokens.spacings.comfortable.medium};
`

const CountGroup = styled.div`
    display: flex;
    align-items: center;
    gap: ${tokens.spacings.comfortable.x_small};
`

const ErrorMessage = styled.div`
    display: flex;
    flex: 1 0 80px;
    align-items: center;
    justify-content: center;
    border: 1px solid ${tokens.colors.interactive.danger__resting.hex};
    background: ${tokens.colors.interactive.danger__highlight.hex};
    border-radius: 4px;
    color: ${tokens.colors.interactive.danger__text.hex};
    text-align: center;
    padding: 16px;
`

const PlaceholderMessage = styled.div`
    display: flex;
    flex: 1 0 80px;
    align-items: center;
    justify-content: center;
    border: 1px dashed ${tokens.colors.ui.background__medium.hex};
    border-radius: 4px;
    color: ${tokens.colors.text.static_icons__tertiary.hex};
    text-align: center;
    padding: 16px;
`

interface DashboardAlertPanelProps {
    alerts: AnalysisEvaluation[]
    severityCounts: Record<AnalysisSeverity, number>
    hasLoadingError?: boolean
}

export const DashboardAlertPanel = ({ alerts, severityCounts, hasLoadingError = false }: DashboardAlertPanelProps) => {
    const { TranslateText } = useLanguageContext()

    // An empty list and a failed request look identical otherwise, which hides a
    // broken analysis feed behind a reassuring "no alarms".
    if (hasLoadingError)
        return (
            <AlertPanelCard>
                <PanelHeader>
                    <Typography variant="h4">{TranslateText('Active alarms')}</Typography>
                </PanelHeader>
                <ErrorMessage>
                    <Typography variant="body_short">{TranslateText('Could not load analysis results')}</Typography>
                </ErrorMessage>
            </AlertPanelCard>
        )

    return (
        <AlertPanelCard>
            <PanelHeader>
                <Typography variant="h4">{TranslateText('Active alarms')}</Typography>
                <CountGroup>
                    {severityCounts[AnalysisSeverity.Alarm] > 0 && (
                        <SeverityCountChip
                            severity={AnalysisSeverity.Alarm}
                            count={severityCounts[AnalysisSeverity.Alarm]}
                        />
                    )}
                    {severityCounts[AnalysisSeverity.Warning] > 0 && (
                        <SeverityCountChip
                            severity={AnalysisSeverity.Warning}
                            count={severityCounts[AnalysisSeverity.Warning]}
                        />
                    )}
                </CountGroup>
            </PanelHeader>
            {alerts.length === 0 ? (
                <PlaceholderMessage>
                    <Typography variant="body_short">{TranslateText('There are no alert messages here')}</Typography>
                </PlaceholderMessage>
            ) : (
                <AlertList>
                    {alerts.map((evaluation, index) => (
                        <SaraAlertCard key={evaluation.analysisId} order={index + 1} evaluation={evaluation} />
                    ))}
                    <PlaceholderMessage>
                        <Typography variant="body_short">
                            {TranslateText('There are no further alerts that need follow-up')}
                        </Typography>
                    </PlaceholderMessage>
                </AlertList>
            )}
        </AlertPanelCard>
    )
}
