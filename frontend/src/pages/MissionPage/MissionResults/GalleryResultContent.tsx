import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { AnalysisFeedback } from '../AnalysisFeedback'
import { ResultAnalysis } from './ResultAnalysis'
import { ResultMediaView } from './ResultMediaView'
import { AnalysisContent, Companion, GalleryBody, Metadata, PreviewButton } from './MissionResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { MissionResult, ResultFocus } from './MissionResults'

interface Props {
    result: MissionResult
    focus: ResultFocus
    installationName: string
    robotName: string | undefined
    onFocus: (focus: ResultFocus) => void
}

export const GalleryResultContent = ({ result, focus, installationName, robotName, onFocus }: Props) => {
    const { TranslateText } = useLanguageContext()
    const { inspection } = result
    const showingAnalysis = focus === 'analysis'

    const feedback =
        result.hasAnalysis && inspection.analysisRunId ? (
            <AnalysisFeedback
                key={inspection.analysisRunId}
                inspectionId={inspection.inspectionId}
                analysisRunId={inspection.analysisRunId}
                feedback={inspection.feedback}
                compact
            />
        ) : null

    if (result.measurementId) {
        return (
            <>
                <GalleryBody $paired={false}>
                    <AnalysisContent $hasFinding={showingAnalysis ? result.hasFinding : undefined}>
                        <Typography variant="h6">
                            {TranslateText(showingAnalysis ? 'Analysis result' : 'Inspection result')}
                        </Typography>
                        <MeasurementDisplay recordId={result.measurementId!} isCompact={false} />
                        {feedback}
                    </AnalysisContent>
                </GalleryBody>
                <Metadata>
                    {(
                        [
                            ['Installation', installationName],
                            ['Robot', robotName],
                            ['Timestamp', inspection.createdAt ? formatDateTime(inspection.createdAt) : ''],
                        ] as const
                    )
                        .filter(([, value]) => value)
                        .map(([label, value]) => (
                            <div key={label}>
                                <Typography variant="caption">{TranslateText(label)}</Typography>
                                <Typography variant="body_short">{value}</Typography>
                            </div>
                        ))}
                </Metadata>
            </>
        )
    }

    const primary = showingAnalysis ? result.analysisMedia : result.rawMedia
    const inspectionCompanion = showingAnalysis ? result.rawMedia : undefined
    const analysisCompanion = showingAnalysis ? undefined : result.analysisMedia
    const hasSummary =
        (inspection.value && inspection.value !== '') ||
        result.hasFinding ||
        (inspection.confidence !== undefined && inspection.confidence !== null)
    const summary = result.hasAnalysis && primary && hasSummary ? <ResultAnalysis inspection={inspection} /> : null
    const showAnalysisCompanion = Boolean(analysisCompanion || summary)
    const hasSidebar = Boolean(inspectionCompanion || showAnalysisCompanion)

    return (
        <>
            <GalleryBody $paired={hasSidebar}>
                <AnalysisContent $hasFinding={showingAnalysis ? result.hasFinding : undefined}>
                    <Typography variant="h6">
                        {TranslateText(showingAnalysis ? 'Analysis result' : 'Inspection result')}
                    </Typography>
                    {primary ? (
                        <ResultMediaView
                            key={`${inspection.inspectionId}-${focus}`}
                            media={primary}
                            label={inspection.tag}
                            size="large"
                            sensorType={result.sensorType}
                        />
                    ) : (
                        <ResultAnalysis inspection={inspection} />
                    )}
                    {!hasSidebar && feedback}
                </AnalysisContent>
                {hasSidebar && (
                    <Companion>
                        {inspectionCompanion && (
                            <>
                                <Typography variant="h6">{TranslateText('Inspection result')}</Typography>
                                <PreviewButton
                                    variant="ghost"
                                    aria-label={TranslateText('Enlarge inspection for {0}', [inspection.tag])}
                                    onClick={() => onFocus('inspection')}
                                >
                                    <ResultMediaView
                                        media={inspectionCompanion}
                                        label={inspection.tag}
                                        sensorType={result.sensorType}
                                    />
                                </PreviewButton>
                            </>
                        )}
                        {showAnalysisCompanion && (
                            <AnalysisContent $hasFinding={result.hasFinding}>
                                <Typography variant="h6">{TranslateText('Analysis result')}</Typography>
                                {analysisCompanion && (
                                    <PreviewButton
                                        variant="ghost"
                                        aria-label={TranslateText('Enlarge analysis for {0}', [inspection.tag])}
                                        onClick={() => onFocus('analysis')}
                                    >
                                        <ResultMediaView
                                            media={analysisCompanion}
                                            label={inspection.tag}
                                            sensorType={result.sensorType}
                                        />
                                    </PreviewButton>
                                )}
                                {summary &&
                                    (!showingAnalysis && !analysisCompanion ? (
                                        <PreviewButton
                                            variant="ghost"
                                            aria-label={TranslateText('Enlarge analysis for {0}', [inspection.tag])}
                                            onClick={() => onFocus('analysis')}
                                        >
                                            {summary}
                                        </PreviewButton>
                                    ) : (
                                        summary
                                    ))}
                            </AnalysisContent>
                        )}
                        {feedback}
                    </Companion>
                )}
            </GalleryBody>
            <Metadata>
                {(
                    [
                        ['Installation', installationName],
                        ['Robot', robotName],
                        ['Timestamp', inspection.createdAt ? formatDateTime(inspection.createdAt) : ''],
                    ] as const
                )
                    .filter(([, value]) => value)
                    .map(([label, value]) => (
                        <div key={label}>
                            <Typography variant="caption">{TranslateText(label)}</Typography>
                            <Typography variant="body_short">{value}</Typography>
                        </div>
                    ))}
            </Metadata>
        </>
    )
}
