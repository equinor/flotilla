import { Icon, Typography } from '@equinor/eds-core-react'
import { Task } from 'models/Task'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import {
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledInfoContent,
} from 'pages/InspectionReportPage/InspectionStyles'
import {
    LargeImageTextPlaceholder,
    LargeImagePendingPlaceholder,
} from 'pages/InspectionReportPage/InspectionReportImage'
import styled from 'styled-components'
import { useInspectionId } from 'pages/InspectionReportPage/SetInspectionIdHook'

interface InspectionDialogViewProps {
    selectedAnalysisId: string
    tasks: Task[]
}
const StyledImage = styled.img<{ $otherContentHeight?: string }>`
    max-height: calc(60vh - ${(props) => props.$otherContentHeight});
    max-width: 100%;
    border: none;
`
const AnalysisImage = ({ inspectionId }: { inspectionId: string }) => {
    const { fetchAnalysisData } = useInspectionsContext()
    const { data, isPending } = fetchAnalysisData(inspectionId)

    if (isPending) return <LargeImagePendingPlaceholder />
    if (!data) return <LargeImageTextPlaceholder errorMessage="No inspection could be found" />

    return <StyledImage $otherContentHeight="0px" src={data} />
}

export const AnalysisResultDialogView = ({ selectedAnalysisId, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedAnalysisId } = useInspectionId()

    const onClose = () => switchSelectedAnalysisId(undefined)

    const currentTask = tasks.find((t) => t.inspection.isarInspectionId == selectedAnalysisId)

    if (!currentTask) {
        return (
            <StyledDialog open={true} isDismissable onClose={onClose}>
                <StyledDialogContent>
                    <StyledDialogInspectionView>
                        <LargeImageTextPlaceholder errorMessage="No analysis could be found" />
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
                        {TranslateText('Analysis result for task') + ' ' + (currentTask.taskOrder + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={onClose}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        {currentTask.inspection.analysisResult?.storageAccount ? (
                            <AnalysisImage inspectionId={currentTask.inspection.isarInspectionId} />
                        ) : (
                            <>{/* No image to display*/}</>
                        )}
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{currentTask.tagId}</Typography>
                            </StyledInfoContent>
                            {currentTask.description && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">{currentTask.description}</Typography>
                                </StyledInfoContent>
                            )}
                            {currentTask.endTime && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(currentTask.endTime, 'dd.MM.yy - HH:mm')}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {currentTask.inspection.analysisResult?.warning && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Warning') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {currentTask.inspection.analysisResult.warning}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {currentTask.inspection.analysisResult?.confidence &&
                                currentTask.inspection.analysisResult?.unit && (
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Confidence') + ':'}</Typography>
                                        <Typography variant="body_short">
                                            {currentTask.inspection.analysisResult.confidence + '%'}
                                        </Typography>
                                    </StyledInfoContent>
                                )}
                        </StyledBottomContent>
                    </div>
                </StyledDialogInspectionView>
            </StyledDialogContent>
        </StyledDialog>
    )
}
