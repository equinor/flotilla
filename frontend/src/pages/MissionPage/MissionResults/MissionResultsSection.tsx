import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { MissionResult, ResultFocus } from 'pages/MissionPage/MissionResults/missionResultPresentation'
import { formatDateTime } from 'utils/StringFormatting'
import { ResultMediaView } from 'pages/MissionPage/MissionResults/ResultMediaView'
import { ResultAnalysis } from 'pages/MissionPage/MissionResults/ResultAnalysis'
import {
    AnalysisPreview,
    CardDetails,
    CardHeading,
    Deck,
    MeasurementPreviewButton,
    PreviewButton,
    ResultCard,
    ResultsSection,
    TaskNumber,
} from 'pages/MissionPage/MissionResults/MissionResultStyles'

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
                {results.map(({ inspection, source, analysis, hasAnalysis, hasFinding, taskNumber, sensorType }) => {
                    const SourcePreviewButton =
                        source?.type === 'measurement' ? MeasurementPreviewButton : PreviewButton
                    return (
                        <ResultCard key={inspection.inspectionId} $hasAnalysis={hasAnalysis}>
                            <CardDetails>
                                <CardHeading>
                                    <Typography variant="h5">
                                        {inspection.tag || TranslateText('Result task {0}', [String(taskNumber)])}
                                    </Typography>
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
                                <SourcePreviewButton
                                    variant="ghost"
                                    aria-label={TranslateText('Enlarge inspection for {0}', [inspection.tag])}
                                    onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                                >
                                    <ResultMediaView media={source} label={inspection.tag} sensorType={sensorType} />
                                </SourcePreviewButton>
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
                    )
                })}
            </Deck>
        </ResultsSection>
    )
}
