import { Typography } from '@equinor/eds-core-react'
import { useLanguageContext } from 'contexts/LanguageContext'
import { ResultMediaView } from 'pages/MissionPage/MissionResults/ResultMediaView'
import {
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
import { InspectionData } from 'models/InspectionRecord'

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

                    if (!rawPreviewButton && !imageView) return <></>
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
