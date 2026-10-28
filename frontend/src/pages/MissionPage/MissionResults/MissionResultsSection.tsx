import { EdsProvider, Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ResultMediaView } from 'pages/MissionPage/MissionResults/ResultMediaView'
import {
    CardDetails,
    CardHeading,
    Deck,
    MeasurementPreviewButton,
    MeasurementTablePreviewButton,
    PreviewButton,
    ResultCard,
    ResultsSection,
    TaskNumber,
} from 'pages/MissionPage/MissionResults/MissionResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { MeasurementTable } from 'components/Displays/MeasurementTable'
import { groupInspectionsByTask, InspectionData } from 'models/InspectionRecord'
import { ResultFocus } from './MissionResults'

export const MissionResultsSection = ({
    data,
    onSelect,
}: {
    data: InspectionData[]
    onSelect: (id: string, focus: ResultFocus) => void
}) => {
    const { TranslateText } = useLanguageContext()
    if (data.length === 0) return null

    const results = groupInspectionsByTask(data)

    return (
        <ResultsSection>
            <Typography variant="h4">{TranslateText('Inspection results')}</Typography>
            <Deck>
                {results.map(({ inspection, measurements }, index) => {
                    const rawPreviewButton =
                        measurements.length > 1 ? (
                            <MeasurementTablePreviewButton
                                variant="ghost"
                                onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                            >
                                <EdsProvider density="compact">
                                    <MeasurementTable measurements={measurements} />
                                </EdsProvider>
                            </MeasurementTablePreviewButton>
                        ) : measurements.length === 1 ? (
                            <MeasurementPreviewButton
                                variant="ghost"
                                onClick={() => onSelect(inspection.inspectionId, 'inspection')}
                            >
                                <MeasurementDisplay recordId={measurements[0].recordId} isCompact={false} />
                            </MeasurementPreviewButton>
                        ) : undefined

                    const imageView = inspection.visualizedSAS ? (
                        <PreviewButton
                            $hasFinding={Boolean(inspection?.warning)}
                            variant="ghost"
                            onClick={() => onSelect(inspection.inspectionId, 'analysis')}
                        >
                            <ResultMediaView
                                source={inspection.visualizedSAS}
                                label={inspection.tag}
                                fileType={inspection.fileType}
                            />
                        </PreviewButton>
                    ) : inspection.anonymizedSAS ? (
                        <PreviewButton variant="ghost" onClick={() => onSelect(inspection.inspectionId, 'inspection')}>
                            <ResultMediaView
                                source={inspection.anonymizedSAS}
                                label={inspection.tag}
                                fileType={inspection.fileType}
                            />
                        </PreviewButton>
                    ) : undefined

                    if (!rawPreviewButton && !imageView) return undefined
                    return (
                        <ResultCard key={inspection.inspectionId}>
                            <CardDetails $hasFinding={Boolean(inspection?.warning)}>
                                <CardHeading>
                                    <TaskNumber variant="caption">
                                        {TranslateText('Index') + ' ' + String(index + 1)}
                                    </TaskNumber>
                                    <Typography bold>{inspection.tag || ''}</Typography>
                                </CardHeading>
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
