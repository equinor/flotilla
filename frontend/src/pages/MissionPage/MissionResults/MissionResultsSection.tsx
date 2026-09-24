import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { MissionResult, ResultFocus } from './missionResultPresentation'
import { formatDateTime } from 'utils/StringFormatting'
import { ResultMediaView } from './ResultMediaView'
import { ResultAnalysis } from './ResultAnalysis'
import {
    AnalysisPreview,
    CardDetails,
    CardHeading,
    Deck,
    PreviewButton,
    ResultCard,
    ResultTitle,
    ResultsSection,
    TaskNumber,
} from './MissionResultStyles'

export const MissionResultsSection = ({
    results,
    onSelect,
}: {
    results: MissionResult[]
    onSelect: (id: string, focus: ResultFocus) => void
}) => {
    const { TranslateText } = useLanguageContext()
    if (results.length === 0) return null

    return (
        <ResultsSection aria-label={TranslateText('Inspection results')}>
            <Typography variant="h4">{TranslateText('Inspection results')}</Typography>
            <Deck>
                {results.map(({ inspection, source, analysis, hasAnalysis, hasFinding, taskNumber, sensorType }) => (
                    <ResultCard key={inspection.inspectionId} $hasAnalysis={hasAnalysis}>
                        <CardDetails>
                            <CardHeading>
                                <ResultTitle variant="h5">
                                    {inspection.tag || TranslateText('Result task {0}', [String(taskNumber)])}
                                </ResultTitle>
                                <TaskNumber variant="caption">
                                    {TranslateText('Result task {0}', [String(taskNumber)])}
                                </TaskNumber>
                            </CardHeading>
                            {inspection.inspectionDescription && (
                                <Typography variant="body_long">{inspection.inspectionDescription}</Typography>
                            )}
                            {inspection.createdAt && (
                                <Typography variant="caption">{formatDateTime(inspection.createdAt)}</Typography>
                            )}
                        </CardDetails>
                        {source && (
                            <PreviewButton
                                variant="ghost"
                                aria-label={TranslateText('Enlarge inspection for {0}', [inspection.tag])}
                                onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                            >
                                <ResultMediaView media={source} label={inspection.tag} sensorType={sensorType} />
                            </PreviewButton>
                        )}
                        {hasAnalysis && (
                            <AnalysisPreview $hasFinding={hasFinding}>
                                <Typography variant="caption">{TranslateText('Analysis result')}</Typography>
                                <PreviewButton
                                    variant="ghost"
                                    aria-label={TranslateText('Enlarge analysis for {0}', [inspection.tag])}
                                    onClick={() => onSelect(inspection.inspectionId, 'analysis')}
                                >
                                    {analysis && (
                                        <ResultMediaView
                                            media={analysis}
                                            label={inspection.tag}
                                            size="analysis"
                                            sensorType={sensorType}
                                        />
                                    )}
                                    <ResultAnalysis inspection={inspection} />
                                </PreviewButton>
                            </AnalysisPreview>
                        )}
                    </ResultCard>
                ))}
            </Deck>
        </ResultsSection>
    )
}
