import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { AnalysisFeedback } from '../AnalysisFeedback'
import { ResultAnalysis } from './ResultAnalysis'
import { ResultMediaView } from './ResultMediaView'
import { AnalysisContent, Companion, GalleryBody, Metadata, PreviewButton } from './MissionResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { MeasurementTable } from 'components/Displays/MeasurementTable'
import { ResultFocus } from './MissionResults'
import { hasInspectionAnalysis, InspectionData, TaskMeasurement } from 'models/InspectionRecord'

interface Props {
    inspection: InspectionData
    measurements: TaskMeasurement[]
    focus: ResultFocus
    installationName: string
    onFocus: (focus: ResultFocus) => void
}

const MeasurementGalleryResultContent = ({
    inspection,
    measurements,
    showAnalysis,
    children,
}: {
    inspection: InspectionData
    measurements: TaskMeasurement[]
    showAnalysis: boolean
    children: React.ReactNode
}) => {
    const { TranslateText } = useLanguageContext()
    const [first] = measurements
    if (!first) return null
    return (
        <>
            <GalleryBody $paired={false}>
                <AnalysisContent $hasFinding={showAnalysis ? Boolean(inspection?.warning) : undefined}>
                    <Typography variant="h6">
                        {TranslateText(showAnalysis ? 'Analysis result' : 'Inspection result')}
                    </Typography>
                    {measurements.length > 1 ? (
                        <MeasurementTable measurements={measurements} />
                    ) : (
                        <MeasurementDisplay recordId={first.recordId} isCompact={false} />
                    )}
                    {children}
                </AnalysisContent>
            </GalleryBody>
        </>
    )
}

export const GalleryResultContent = ({ inspection, measurements, focus, installationName, onFocus }: Props) => {
    const { TranslateText } = useLanguageContext()
    const showingAnalysis = focus === 'analysis'

    const hasFinding = Boolean(inspection?.warning)
    const hasAnalysis = hasInspectionAnalysis(inspection)

    const feedback =
        hasAnalysis && inspection.analysisRunId ? (
            <AnalysisFeedback
                key={inspection.analysisRunId}
                inspectionId={inspection.inspectionId}
                analysisRunId={inspection.analysisRunId}
                feedback={inspection.feedback}
                compact
            />
        ) : null

    const metadata = (
        <Metadata>
            {(
                [
                    ['Installation', installationName],
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
    )

    if (measurements.length > 0) {
        return (
            <>
                <MeasurementGalleryResultContent
                    inspection={inspection}
                    measurements={measurements}
                    showAnalysis={showingAnalysis}
                >
                    {feedback}
                </MeasurementGalleryResultContent>
                {metadata}
            </>
        )
    }

    const primarySource = showingAnalysis ? inspection.visualizedSAS : inspection.anonymizedSAS
    const inspectionCompanion = showingAnalysis ? inspection.anonymizedSAS : undefined
    const analysisCompanion = showingAnalysis ? undefined : inspection.visualizedSAS
    const hasSummary =
        (inspection.value && inspection.value !== '') ||
        hasFinding ||
        (inspection.confidence !== undefined && inspection.confidence !== null)
    const summary = hasAnalysis && primarySource && hasSummary ? <ResultAnalysis inspection={inspection} /> : null
    const showAnalysisCompanion = Boolean(analysisCompanion || summary)
    const hasSidebar = Boolean(inspectionCompanion || showAnalysisCompanion)

    return (
        <>
            <GalleryBody $paired={hasSidebar}>
                <AnalysisContent $hasFinding={showingAnalysis ? hasFinding : undefined}>
                    <Typography variant="h6">
                        {TranslateText(showingAnalysis ? 'Analysis result' : 'Inspection result')}
                    </Typography>
                    {primarySource ? (
                        <ResultMediaView
                            key={`${inspection.inspectionId}-${focus}`}
                            source={primarySource}
                            label={inspection.tag}
                            size="large"
                            fileType={inspection.fileType}
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
                                <PreviewButton variant="ghost" onClick={() => onFocus('inspection')}>
                                    <ResultMediaView
                                        source={inspectionCompanion}
                                        label={inspection.tag}
                                        fileType={inspection.fileType}
                                    />
                                </PreviewButton>
                            </>
                        )}
                        {showAnalysisCompanion && (
                            <AnalysisContent $hasFinding={hasFinding}>
                                <Typography variant="h6">{TranslateText('Analysis result')}</Typography>
                                {analysisCompanion && (
                                    <PreviewButton variant="ghost" onClick={() => onFocus('analysis')}>
                                        <ResultMediaView
                                            source={analysisCompanion}
                                            label={inspection.tag}
                                            fileType={inspection.fileType}
                                        />
                                    </PreviewButton>
                                )}
                                {summary &&
                                    (!showingAnalysis && !analysisCompanion ? (
                                        <PreviewButton variant="ghost" onClick={() => onFocus('analysis')}>
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
            {metadata}
        </>
    )
}
