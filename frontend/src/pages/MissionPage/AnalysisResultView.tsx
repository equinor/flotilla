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
    LargeImageErrorPlaceholder,
    LargeImagePendingPlaceholder,
} from 'pages/InspectionReportPage/InspectionReportImage'
import styled from 'styled-components'

interface InspectionDialogViewProps {
    selectedTask: Task
    onClose: () => void
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
    if (!data) return <LargeImageErrorPlaceholder errorMessage="No inspection could be found" />

    return <StyledImage $otherContentHeight="0px" src={data} />
}

export const AnalysisResultDialogView = ({ selectedTask, onClose }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()

    return (
        <StyledDialog open={true} isDismissable onClose={onClose}>
            <StyledDialogContent>
                <StyledDialogHeader>
                    <Typography variant="accordion_header" group="ui">
                        {TranslateText('Analysis result for task') + ' ' + (selectedTask.taskOrder + 1)}
                    </Typography>
                    <StyledCloseButton variant="ghost" onClick={onClose}>
                        <Icon name={Icons.Clear} size={24} />
                    </StyledCloseButton>
                </StyledDialogHeader>
                <StyledDialogInspectionView>
                    <div>
                        <AnalysisImage inspectionId={selectedTask.inspection.isarInspectionId} />
                        <StyledBottomContent>
                            <StyledInfoContent>
                                <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                <Typography variant="body_short">{selectedTask.tagId}</Typography>
                            </StyledInfoContent>
                            {selectedTask.description && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Description') + ':'}</Typography>
                                    <Typography variant="body_short">{selectedTask.description}</Typography>
                                </StyledInfoContent>
                            )}
                            {selectedTask.endTime && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Timestamp') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {formatDateTime(selectedTask.endTime, 'dd.MM.yy - HH:mm')}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {selectedTask.inspection.analysisResult?.warning && (
                                <StyledInfoContent>
                                    <Typography variant="caption">{TranslateText('Warning') + ':'}</Typography>
                                    <Typography variant="body_short">
                                        {selectedTask.inspection.analysisResult.warning}
                                    </Typography>
                                </StyledInfoContent>
                            )}
                            {selectedTask.inspection.analysisResult?.confidence &&
                                selectedTask.inspection.analysisResult?.unit && (
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Confidence') + ':'}</Typography>
                                        <Typography variant="body_short">
                                            {selectedTask.inspection.analysisResult.confidence + '%'}
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
