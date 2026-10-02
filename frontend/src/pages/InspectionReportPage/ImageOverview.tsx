import { useLanguageContext } from 'contexts/LanguageContext'
import {
    StyledImageCard,
    StyledImagesSection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionOverviewDialogView,
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

export const InspectionOverviewDialogView = ({ inspectionData }: { inspectionData: InspectionData[] }) => (
    <OverviewDialogView inspectionData={inspectionData} variant="inspection" />
)

export const AnalysisOverviewDialogView = ({ inspectionData }: { inspectionData: InspectionData[] }) => (
    <OverviewDialogView inspectionData={inspectionData} variant="analysis" />
)
