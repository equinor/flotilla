import { Button, Card, Dialog } from '@equinor/eds-core-react'
import styled from 'styled-components'

export const StyledDict = {
    FormCard: styled(Card)`
        display: flex;
        justify-content: center;
        padding: 8px;
        gap: 25px;
    `,

    ButtonSection: styled.div`
        display: flex;
        margin-left: auto;
        gap: 10px;
    `,

    FormContainer: styled.div`
        display: grid;
        grid-template-columns: [c1] 1fr [c2] 1fr [c3] 1fr;
        grid-template-rows: [r1] auto [r1] auto [r1] auto;
        align: left;
        align-items: flex-start;
        align-content: flex-start;
        max-width: 1200px;
        min-width: 600px;
        gap: 10px 20px;
    `,

    FormItem: styled.div`
        width: 100%;
        height: auto;
        padding: 5px;
        word-break: break-word;
        hyphens: auto;
        min-height: 60px;
    `,

    Dialog: styled(Dialog)`
        display: flex;
        justify-content: space-between;
        padding: 1rem;
        width: 620px;
    `,

    MissionDefinitionPage: styled.div`
        display: flex;
        flex-wrap: wrap;
        justify-content: start;
        flex-direction: column;
        gap: 1rem;
        margin: 2rem;
    `,

    Button: styled(Button)`
        width: 260px;
    `,

    InspectionFrequencyDiv: styled.div`
        padding: 10px;
    `,

    TitleComponent: styled.div`
        display: flex;
        align-items: center;
        height: 36px;
    `,

    EditButton: styled(Button)`
        padding-left: 5px;
    `,

    Card: styled(Card)`
        display: flex;
        padding: 8px;
        height: 110px;
    `,
    HeaderSection: styled.div`
        display: flex;
        flex-direction: column;
        gap: 0.4rem;
    `,

    TitleSection: styled.div`
        display: flex;
        align-items: center;
        gap: 10px;
    `,
}
