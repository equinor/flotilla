import { Typography } from '@equinor/eds-core-react'
import { tokens } from '@equinor/eds-tokens'
import { AnalysisValueDisplay } from 'components/Displays/TaskDisplay'
import { useLanguageContext } from 'contexts/LanguageContext'
import { hasResultValue, InspectionData } from 'models/InspectionRecord'
import styled from 'styled-components'

const Content = styled.div`
    display: flex;
    flex-direction: column;
    gap: ${tokens.spacings.comfortable.small};
    overflow-wrap: anywhere;
`

export const ResultAnalysis = ({ inspection }: { inspection: InspectionData }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <Content>
            {hasResultValue(inspection.value) && inspection.value !== undefined && (
                <AnalysisValueDisplay
                    value={inspection.value}
                    unit={inspection.unit}
                    analysisType={inspection.analysisType}
                    presentation="result"
                />
            )}
            {inspection.warning && (
                <Typography variant="body_short">
                    {TranslateText('Warning')}: {inspection.warning}
                </Typography>
            )}
            {inspection.confidence !== undefined && inspection.confidence !== null && (
                <Typography variant="caption">
                    {TranslateText('Confidence')}: {Math.round(inspection.confidence)} %
                </Typography>
            )}
        </Content>
    )
}
