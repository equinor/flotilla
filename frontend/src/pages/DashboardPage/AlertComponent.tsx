import { Chip, Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { useId } from 'react'
import styled from 'styled-components'
import { useLanguageContext } from 'contexts/LanguageContext'
import { AnalysisEvaluation } from 'models/analysis/AnalysisEvaluation'
import { formatMeasurement } from 'models/analysis/AnalysisMeasurement'
import { formatDateTime } from 'utils/StringFormatting'
import { severityStyles } from './SeverityIndicator'

const StyledRow = styled.div<{ $accent: string }>`
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: ${tokens.spacings.comfortable.xxx_large};
    min-width: 0;
    padding: ${tokens.spacings.comfortable.large} 0;
    padding-left: ${tokens.spacings.comfortable.large};
    border-left: 5px solid ${({ $accent }) => $accent};
    border-bottom: 1px dashed ${tokens.colors.ui.background__medium.hex};
    overflow-wrap: anywhere;

    &:last-child {
        border-bottom: none;
    }
`
const Identity = styled.div`
    display: flex;
    flex-direction: column;
    min-width: 0;
    gap: ${tokens.spacings.comfortable.x_small};
`
const TitleRow = styled.div`
    display: flex;
    align-items: center;
    gap: ${tokens.spacings.comfortable.medium};
`
const OrderBadge = styled.span`
    display: flex;
    align-items: center;
    justify-content: center;
    width: 28px;
    height: 28px;
    border-radius: 50%;
    border: 1px solid black;
    font-family: Equinor, sans-serif;
    font-weight: 400;
`
const AnalysisLabel = styled(Typography)`
    font-size: 1.1rem;
`
const WarningSection = styled.div`
    display: flex;
    flex-direction: column;
    align-items: flex-end;
    flex-shrink: 0;
    gap: ${tokens.spacings.comfortable.medium};
`
const WarningBadge = styled(Chip)<{ $accent: string; $background: string }>`
    background-color: ${({ $background }) => $background};
    color: ${({ $accent }) => $accent};
    height: 32px;
    font-size: 1rem;
    padding-inline: 16px;
`
const ThresholdCaption = styled(Typography)`
    color: ${tokens.colors.text.static_icons__tertiary.hex};
`
interface SaraAlertCardProps {
    evaluation: AnalysisEvaluation
    order: number
}
export const SaraAlertCard = ({ evaluation, order }: SaraAlertCardProps) => {
    const { TranslateText } = useLanguageContext()
    const titleId = useId()
    const style = severityStyles[evaluation.severity]

    // The measured value explains the alert better than the severity alone, but
    // analyses without an extractor only have SARA's own wording to fall back on.
    // One decimal, so a value near a limit is not rounded onto the wrong side of it.
    const detail = evaluation.measurement
        ? formatMeasurement(evaluation.measurement, 1)
        : (evaluation.providerWarning?.trim() ?? '')

    return (
        <StyledRow role="article" aria-labelledby={titleId} $accent={style.color}>
            <Identity>
                <TitleRow>
                    <Typography id={titleId} as="h3" variant="h4">
                        {evaluation.subject.tag}
                    </Typography>
                    <OrderBadge>{order}</OrderBadge>
                </TitleRow>
                <AnalysisLabel variant="body_short">{TranslateText(evaluation.analysisLabel)}</AnalysisLabel>
            </Identity>
            <WarningSection>
                <WarningBadge $accent={style.color} $background={style.backgroundColor}>
                    {detail ? `${TranslateText(style.label)} - ${detail}` : TranslateText(style.label)}
                </WarningBadge>
                {evaluation.thresholdDescription && (
                    <ThresholdCaption variant="caption">{evaluation.thresholdDescription}</ThresholdCaption>
                )}
                <Typography variant="caption">{formatDateTime(evaluation.observedAt)}</Typography>
            </WarningSection>
        </StyledRow>
    )
}
