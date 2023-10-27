import React from "react";
import Draft from 'Admin/CanvasEditor/DraftJs/Draft.min.js';

const findLinkEntities = (contentBlock, callback, contentState) => {
    contentBlock.findEntityRanges((character) => {
        const entityKey = character.getEntity();
        return (
            entityKey !== null &&
            contentState.getEntity(entityKey).getType() === "LINK"
        );
    }, callback);
};


const Link = ({ entityKey, contentState, children }) => {
    let data = contentState.getEntity(entityKey).getData();
    let url = data.url ? data.url : data.href;
    return (
        <a
            href={url}
            target="_blank"
        >
            {children}
        </a>
    );
};

export const createLinkDecorator = () => {
    return new Draft.CompositeDecorator ([
    {
        strategy: findLinkEntities,
        component: Link,
    },
])};