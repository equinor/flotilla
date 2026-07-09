import { useLanguageContext } from 'components/Contexts/LanguageContext'
import {
    StyledImageCard,
    StyledImagesSection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionOverviewDialogView,
    StyledInspectionOverviewSection,
} from './InspectionStyles'
import { Typography } from '@equinor/eds-core-react'
import { formatDateTime } from 'utils/StringFormatting'
import { SmallAnalysisResult, SmallInspectionResult } from 'pages/InspectionReportPage/InspectionReportImage'
import { useInspectionId } from './SetInspectionIdHook'
import { InspectionData } from 'models/InspectionRecord'

type OverviewVariant = 'inspection' | 'analysis'

const ImageOverview = ({ inspectionData, variant }: { inspectionData: InspectionData[]; variant: OverviewVariant }) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionId, switchSelectedAnalysisId } = useInspectionId()

    const onSelect = variant === 'analysis' ? switchSelectedAnalysisId : switchSelectedInspectionId
    const renderImage = (inspection: InspectionData) =>
        variant === 'analysis' ? (
            <SmallAnalysisResult inspectionId={inspection.inspectionId} />
        ) : (
            <SmallInspectionResult inspection={inspection} />
        )

    return (
        <StyledImagesSection>
            <StyledInspectionCards>
                {inspectionData.map((inspection) => (
                    <StyledImageCard key={inspection.inspectionId} onClick={() => onSelect(inspection.inspectionId)}>
                        {renderImage(inspection)}
                        <StyledInspectionData>
                            {inspection.tag && (
                                <StyledInspectionContent>
                                    <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                    <Typography variant="body_short">{inspection.tag}</Typography>
                                </StyledInspectionContent>
                            )}
                            {inspection.createdAt && (
                                <StyledInspectionContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">{formatDateTime(inspection.createdAt)}</Typography>
                                </StyledInspectionContent>
                            )}
                        </StyledInspectionData>
                    </StyledImageCard>
                ))}
            </StyledInspectionCards>
        </StyledImagesSection>
    )
}

const OverviewSection = ({
    inspectionData,
    variant,
    title,
}: {
    inspectionData: InspectionData[]
    variant: OverviewVariant
    title: string
}) => (
    <StyledInspectionOverviewSection>
        <Typography variant="h4">{title}</Typography>
        <ImageOverview inspectionData={inspectionData} variant={variant} />
    </StyledInspectionOverviewSection>
)

const OverviewDialogView = ({
    inspectionData,
    variant,
}: {
    inspectionData: InspectionData[]
    variant: OverviewVariant
}) => (
    <StyledInspectionOverviewDialogView>
        <ImageOverview inspectionData={inspectionData} variant={variant} />
    </StyledInspectionOverviewDialogView>
)

export const InspectionOverviewSection = ({ inspectionData }: { inspectionData: InspectionData[] }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <OverviewSection
            inspectionData={inspectionData}
            variant="inspection"
            title={TranslateText('Inspection result')}
        />
    )
}

export const AnalysisOverviewSection = ({ inspectionData }: { inspectionData: InspectionData[] }) => {
    const { TranslateText } = useLanguageContext()
    return (
        <OverviewSection inspectionData={inspectionData} variant="analysis" title={TranslateText('Analysis result')} />
    )
}

export const InspectionOverviewDialogView = ({ inspectionData }: { inspectionData: InspectionData[] }) => (
    <OverviewDialogView inspectionData={inspectionData} variant="inspection" />
)

export const AnalysisOverviewDialogView = ({ inspectionData }: { inspectionData: InspectionData[] }) => (
    <OverviewDialogView inspectionData={inspectionData} variant="analysis" />
)
