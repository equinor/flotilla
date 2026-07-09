import { Icon, Typography } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import {
    HiddenOnSmallScreen,
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledInfoContent,
} from 'pages/InspectionReportPage/InspectionStyles'
import { TextAsImage, PendingResultPlaceholder } from 'pages/InspectionReportPage/InspectionReportImage'
import styled from 'styled-components'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'
import { AnalysisOverviewDialogView } from 'pages/InspectionReportPage/ImageOverview'
import { InspectionData } from 'models/InspectionRecord'

interface InspectionDialogViewProps {
    selectedInspectionId: string
    inspectionData: InspectionData[]
}
const StyledImage = styled.img<{ $otherContentHeight?: string }>`
    max-height: calc(60vh - ${(props) => props.$otherContentHeight});
    max-width: 100%;
    border: none;
`
const AnalysisImage = ({ sasURI, isPending }: { sasURI: string | undefined; isPending: boolean }) => {
    if (isPending) return <PendingResultPlaceholder isLargeImage={true} />
    if (!sasURI) return <TextAsImage isLargeImage={true} text="No inspection could be found" />

    return <StyledImage $otherContentHeight="0px" src={sasURI} />
}

export const AnalysisResultDialogContent = ({ inspection }: { inspection: InspectionData }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <div>
            {inspection.visualisedSAS ? (
                <AnalysisImage sasURI={inspection.visualisedSAS} isPending={false} />
            ) : (
                <>{/* No image to display*/}</>
            )}
            <StyledBottomContent>
                <StyledInfoContent>
                    <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                    <Typography variant="body_short">{inspection.tag}</Typography>
                </StyledInfoContent>
                {inspection.inspectionDescription && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                        <Typography variant="body_short">{inspection.inspectionDescription}</Typography>
                    </StyledInfoContent>
                )}
                {inspection.createdAt && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                        <Typography variant="body_short">{formatDateTime(inspection.createdAt)}</Typography>
                    </StyledInfoContent>
                )}
                {inspection?.warning && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Warning') + ':'}</Typography>
                        <Typography variant="body_short">{inspection.warning}</Typography>
                    </StyledInfoContent>
                )}
                {inspection?.value && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Value') + ':'}</Typography>
                        <Typography variant="body_short">{inspection.value}</Typography>
                    </StyledInfoContent>
                )}
                {inspection?.confidence && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Confidence') + ':'}</Typography>
                        <Typography variant="body_short">{Math.round(inspection.confidence) + '%'}</Typography>
                    </StyledInfoContent>
                )}
            </StyledBottomContent>
        </div>
    )
}

export const AnalysisResultDialogView = ({ selectedInspectionId, inspectionData }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedAnalysisId } = useInspectionId()

    const onClose = () => switchSelectedAnalysisId(undefined)

    const inspectionIndex = inspectionData.findIndex((i) => i.inspectionId == selectedInspectionId)
    const currentInspection = inspectionData[inspectionIndex]

    if (!currentInspection) {
        return (
            <StyledDialog open={true} isDismissable onClose={onClose}>
                <StyledDialogContent>
                    <StyledDialogInspectionView>
                        <TextAsImage isLargeImage={true} text="No analysis could be found" />
                    </StyledDialogInspectionView>
                </StyledDialogContent>
            </StyledDialog>
        )
    }

    return (
        <StyledDialog open={true} isDismissable onClose={onClose}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Analysis result for task') + ' ' + (inspectionIndex + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={onClose}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <AnalysisResultDialogContent inspection={currentInspection} />
                    <HiddenOnSmallScreen>
                        <AnalysisOverviewDialogView inspectionData={inspectionData} />
                    </HiddenOnSmallScreen>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}
