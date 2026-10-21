import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
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
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { MissionResult, ResultFocus } from './MissionResults'

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
                {results.map(
                    ({
                        inspection,
                        rawMedia,
                        analysisMedia,
                        hasAnalysis,
                        hasFinding,
                        taskNumber,
                        sensorType,
                        measurementId,
                    }) => {
                        const rawPreviewButton = measurementId ? (
                            <MeasurementPreviewButton
                                variant="ghost"
                                aria-label={TranslateText('Enlarge inspection for {0}', [inspection.tag])}
                                onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                            >
                                <MeasurementDisplay recordId={measurementId} isCompact={false} />
                            </MeasurementPreviewButton>
                        ) : rawMedia ? (
                            <PreviewButton
                                variant="ghost"
                                aria-label={TranslateText('Enlarge inspection for {0}', [inspection.tag])}
                                onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                            >
                                <ResultMediaView media={rawMedia} label={inspection.tag} sensorType={sensorType} />
                            </PreviewButton>
                        ) : undefined
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
                                        <Typography variant="caption">
                                            {formatDateTime(inspection.createdAt)}
                                        </Typography>
                                    )}
                                </CardDetails>
                                {rawPreviewButton}
                                {hasAnalysis && (
                                    <AnalysisPreview $hasFinding={hasFinding}>
                                        <Typography variant="caption">{TranslateText('Analysis result')}</Typography>
                                        <PreviewButton
                                            variant="ghost"
                                            aria-label={TranslateText('Enlarge analysis for {0}', [inspection.tag])}
                                            onClick={() => onSelect(inspection.inspectionId, 'analysis')}
                                        >
                                            {analysisMedia && (
                                                <ResultMediaView
                                                    media={analysisMedia}
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
                    }
                )}
            </Deck>
        </ResultsSection>
    )
}
