import { Button, Divider, Icon, Typography } from '@equinor/eds-core-react'
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

const FEEDBACK_ICON_SIZE = 24

const StyledImage = styled.img<{ $otherContentHeight?: string }>`
    max-height: calc(60vh - ${(props) => props.$otherContentHeight});
    max-width: 100%;
    border: none;
`

const FeedbackSection = styled.div`
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--eds-spacing-horizontal-md);
    padding: var(--eds-spacing-vertical-md) var(--eds-spacing-horizontal-md);
`

const FeedbackPrompt = styled.div`
    display: flex;
    flex-direction: column;
    gap: var(--eds-spacing-vertical-2xs);
`

const FeedbackButtons = styled.div`
    display: flex;
    flex-shrink: 0;
    gap: var(--eds-spacing-horizontal-sm);
`

const FeedbackButton = styled(Button)`
    color: var(--eds-color-text-subtle);
`

const ThumbsUpButton = styled(FeedbackButton)`
    &[aria-pressed='true'] {
        color: var(--eds-color-text-success-subtle);
        background: var(--eds-color-bg-success-fill-muted-default);

        &:hover {
            background: var(--eds-color-bg-success-fill-muted-hover);
        }
        &:active {
            background: var(--eds-color-bg-success-fill-muted-active);
        }
    }
`

const ThumbsDownButton = styled(FeedbackButton)`
    &[aria-pressed='true'] {
        color: var(--eds-color-text-danger-subtle);
        background: var(--eds-color-bg-danger-fill-muted-default);

        &:hover {
            background: var(--eds-color-bg-danger-fill-muted-hover);
        }
        &:active {
            background: var(--eds-color-bg-danger-fill-muted-active);
        }
    }
`

const AnalysisImage = ({ sasURI, isPending }: { sasURI: string | undefined; isPending: boolean }) => {
    if (isPending) return <PendingResultPlaceholder isLargeImage={true} />
    if (!sasURI) return <TextAsImage isLargeImage={true} text="No inspection could be found" />

    return <StyledImage $otherContentHeight="0px" src={sasURI} />
}

type AnalysisFeedbackValue = 'positive' | 'negative'

interface Props {
    givenFeedback?: AnalysisFeedbackValue
}

// TODO: pass the persisted feedback once the backend exposes it on InspectionData
const AnalysisFeedback = ({ givenFeedback }: Props) => {
    const { TranslateText } = useLanguageContext()

    return (
        <FeedbackSection>
            <FeedbackPrompt>
                <Typography variant="h6">{TranslateText('Feedback')}</Typography>
                <Typography variant="body_short">{TranslateText('Feedback description')}</Typography>
            </FeedbackPrompt>
            <FeedbackButtons>
                <ThumbsUpButton
                    variant="ghost_icon"
                    aria-label={TranslateText('The analysis is correct')}
                    aria-pressed={givenFeedback === 'positive'}
                >
                    <Icon name={Icons.ThumbsUp} size={FEEDBACK_ICON_SIZE} />
                </ThumbsUpButton>
                <ThumbsDownButton
                    variant="ghost_icon"
                    aria-label={TranslateText('Something is wrong with the analysis')}
                    aria-pressed={givenFeedback === 'negative'}
                >
                    <Icon name={Icons.ThumbsDown} size={FEEDBACK_ICON_SIZE} />
                </ThumbsDownButton>
            </FeedbackButtons>
        </FeedbackSection>
    )
}

export const AnalysisResultDialogContent = ({ inspection }: { inspection: InspectionData }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <div>
            {inspection.visualizedSAS ? (
                <AnalysisImage sasURI={inspection.visualizedSAS} isPending={false} />
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
                {inspection?.confidence !== undefined && inspection?.confidence !== null && (
                    <StyledInfoContent>
                        <Typography variant="caption">{TranslateText('Confidence') + ':'}</Typography>
                        <Typography variant="body_short">{Math.round(inspection.confidence) + '%'}</Typography>
                    </StyledInfoContent>
                )}
            </StyledBottomContent>
            <Divider color="light" variant="medium" />
            <AnalysisFeedback givenFeedback="negative" />
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
