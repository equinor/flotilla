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
import { ResultFocus } from './MissionResults'
import { hasInspectionAnalysis, InspectionData } from 'models/InspectionRecord'

export const MissionResultsSection = ({
    data,
    onSelect,
}: {
    data: InspectionData[]
    onSelect: (id: string, focus: ResultFocus) => void
}) => {
    const { TranslateText } = useLanguageContext()
    if (data.length === 0) return null

    return (
        <ResultsSection>
            <Typography variant="h4">{TranslateText('Inspection results')}</Typography>
            <Deck>
                {data.map((inspection, index) => {
                    const rawPreviewButton = inspection.measurementRecordId ? (
                        <MeasurementPreviewButton
                            variant="ghost"
                            onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                        >
                            <MeasurementDisplay recordId={inspection.measurementRecordId} isCompact={false} />
                        </MeasurementPreviewButton>
                    ) : undefined

                    const imageView = inspection.visualizedSAS ? (
                        <AnalysisPreview $hasFinding={Boolean(inspection?.warning)}>
                            <PreviewButton
                                variant="ghost"
                                onClick={() => onSelect(inspection.inspectionId, 'analysis')}
                            >
                                <ResultMediaView
                                    source={inspection.visualizedSAS}
                                    label={inspection.tag}
                                    size="analysis"
                                    fileType={inspection.fileType}
                                />
                                <ResultAnalysis inspection={inspection} isCompact />
                            </PreviewButton>
                        </AnalysisPreview>
                    ) : inspection.anonymizedSAS ? (
                        <PreviewButton variant="ghost" onClick={() => onSelect(inspection.inspectionId, 'inspection')}>
                            <ResultMediaView
                                source={inspection.anonymizedSAS}
                                label={inspection.tag}
                                fileType={inspection.fileType}
                            />
                        </PreviewButton>
                    ) : undefined
                    return (
                        <ResultCard key={inspection.inspectionId} $hasAnalysis={hasInspectionAnalysis(inspection)}>
                            <CardDetails>
                                <CardHeading>
                                    <Typography variant="h5">
                                        {inspection.tag || TranslateText('Result task {0}', [String(index)])}
                                    </Typography>
                                    <TaskNumber variant="caption">
                                        {TranslateText('Result task {0}', [String(index)])}
                                    </TaskNumber>
                                </CardHeading>
                                {inspection.inspectionDescription && (
                                    <Typography variant="body_long">{inspection.inspectionDescription}</Typography>
                                )}
                                {inspection.createdAt && (
                                    <Typography variant="caption">{formatDateTime(inspection.createdAt)}</Typography>
                                )}
                            </CardDetails>
                            {rawPreviewButton}
                            {imageView}
                        </ResultCard>
                    )
                })}
            </Deck>
        </ResultsSection>
    )
}
